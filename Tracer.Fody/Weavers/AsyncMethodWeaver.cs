using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Mono.Collections.Generic;
using System;
using System.Collections.Generic;
using System.Linq;
using Tracer.Fody.Helpers;

namespace Tracer.Fody.Weavers
{
    internal class AsyncMethodWeaver : MethodWeaverBase
    {
        private readonly TypeDefinition _generatedType;
        private FieldReference _tickFieldRef;
        private MethodBody _moveNextBody;
        private MethodDefinition _moveNextDefinition;

        internal AsyncMethodWeaver(TypeReferenceProvider typeReferenceProvider,
            MethodReferenceProvider methodReferenceProvider, ILoggerProvider loggerProvider,
            MethodDefinition methodDefinition)
            : base(typeReferenceProvider, methodReferenceProvider, loggerProvider, methodDefinition)
        {
            CustomAttribute asyncAttribute = methodDefinition.CustomAttributes.Single(it => it.AttributeType.FullName.Equals(_typeReferenceProvider.AsyncStateMachineAttribute.FullName));
            _generatedType = asyncAttribute.ConstructorArguments[0].Value as TypeDefinition;
            WeavingLog.LogDebug($"Weaving {methodDefinition.FullName}");
        }

        protected override void WeaveTraceEnter()
        {
            List<Instruction> instructions = CreateTraceEnterCallInstructions();
            _body.InsertAtTheBeginning(instructions);

            ExtendGeneratedTypeWithTickcountField();
            //pass initial tickcount to generated type

            //search the variable for the state machine in the body
            VariableDefinition genVar = _body.Variables.FirstOrDefault(it => it.VariableType.GetElementType().FullName.Equals(_generatedType.FullName));
            if (genVar == null)
            {
                //this should not happen
                throw new ApplicationException($"Cannot find async statemachine for async method {_methodDefinition.Name}.");
            }

            ILProcessor processor = _body.GetILProcessor();
            List<Instruction> instrs = new List<Instruction>();
            Instruction instr;

            //search the first ldloc or ldloca that uses this variable and insert our param passing block
            if (!_generatedType.IsValueType)
            {
                instr = _body.Instructions.FirstOrDefault(it => it.OpCode == OpCodes.Ldloc && it.Operand == genVar);
                instrs.Add(Instruction.Create(OpCodes.Ldloc, genVar));
            }
            else
            {
                instr = _body.Instructions.FirstOrDefault(it => it.OpCode == OpCodes.Ldloca && it.Operand == genVar);
                instrs.Add(Instruction.Create(OpCodes.Ldloca, genVar));
            }

            instrs.Add(Instruction.Create(OpCodes.Ldloc, StartTickVariable));
            instrs.Add(Instruction.Create(OpCodes.Stfld, _tickFieldRef));
            instr.InsertBefore(processor, instrs);
        }

        protected override void WeaveTraceLeave()
        {
            _moveNextDefinition =
                _generatedType.Methods.Single(it => it.Name.Equals("MoveNext", StringComparison.OrdinalIgnoreCase));
            _moveNextBody = _moveNextDefinition.Body;

            _moveNextBody.SimplifyMacros();

            //find the leave part in the generated async state machine
            Instruction setResultInstr = _moveNextBody.Instructions.FirstOrDefault(IsCallSetResult);
            Instruction setExceptionInstr = _moveNextBody.Instructions.FirstOrDefault(IsCallSetException);

            VariableDefinition returnValueDef = null;
            ILProcessor processor = _moveNextBody.GetILProcessor();

            if (setResultInstr != null) //rarely it might happen that there is not SetResult
            {
                //if we have return value store it in a local var
                if (HasReturnValue)
                {
                    List<Instruction> retvalDupInstructions = CreateReturnValueSavingInstructions(out returnValueDef);
                    setResultInstr.InsertBefore(processor, retvalDupInstructions);
                }

                //do the exit logging
                setResultInstr.InsertBefore(processor, CreateTraceReturnLoggingInstructions(returnValueDef));
            }

            if (setExceptionInstr != null)
            {
                //do the exception exit logging
                VariableDefinition exceptionValueDef = _moveNextBody.DeclareVariable("$exception",
                    _typeReferenceProvider.Exception);

                List<Instruction> exceptionDupInstructions = new List<Instruction>()
                {
                    Instruction.Create(OpCodes.Dup),
                    Instruction.Create(OpCodes.Stloc, exceptionValueDef)
                };

                setExceptionInstr.InsertBefore(processor, exceptionDupInstructions);
                setExceptionInstr.InsertBefore(processor, CreateTraceReturnWithExceptionLoggingInstructions(exceptionValueDef));
            }

            //search and replace static log calls in moveNext
            SearchForAndReplaceStaticLogCallsInMoveNext();

            _moveNextBody.InitLocals = true;
            _moveNextBody.OptimizeMacros();
        }

        private void SearchForAndReplaceStaticLogCallsInMoveNext()
        {
            //look for static log calls
            foreach (Instruction instruction in _moveNextBody.Instructions.ToList()) //create a copy of the instructions so we can update the original
            {
                MethodReference methodReference = instruction.Operand as MethodReference;
                if (instruction.OpCode == OpCodes.Call && methodReference != null && IsStaticLogTypeOrItsInnerType(methodReference.DeclaringType))
                {
                    //change the call
                    if (!methodReference.HasParameters)
                    {
                        ChangeStaticLogCallWithoutParameter(instruction);
                    }
                    else
                    {
                        ChangeStaticLogCallWithParameter(instruction);
                    }
                }
            }
        }

        private void ChangeStaticLogCallWithParameter(Instruction oldInstruction)
        {
            List<Instruction> instructions = new List<Instruction>();
            MethodReference methodReference = (MethodReference)oldInstruction.Operand;
            MethodReferenceInfo methodReferenceInfo = new MethodReferenceInfo(methodReference);

            if (methodReferenceInfo.IsPropertyAccessor() && methodReferenceInfo.IsSetter)
            {
                throw new ApplicationException("Rewriting static property setters is not supported.");
            }

            Collection<ParameterDefinition> parameters = methodReference.Parameters;

            //create variables to store parameters and push values into them
            VariableDefinition[] variables = new VariableDefinition[parameters.Count];

            for (int idx = 0; idx < parameters.Count; idx++)
            {
                variables[idx] = GetVariableDefinitionForType(parameters[idx].ParameterType, methodReference, _moveNextDefinition);
                _moveNextBody.Variables.Add(variables[idx]);
            }

            //store in reverse order
            for (int idx = parameters.Count - 1; idx >= 0; idx--)
            {
                instructions.Add(Instruction.Create(OpCodes.Stloc, variables[idx]));
            }

            //build-up instance call
            instructions.Add(Instruction.Create(OpCodes.Ldsfld, TypeWeaver.CreateLoggerStaticField(_typeReferenceProvider, _methodReferenceProvider, _generatedType)));
            instructions.AddRange(LoadMethodNameOnStack());

            for (int idx = 0; idx < parameters.Count; idx++)
            {
                instructions.Add(Instruction.Create(OpCodes.Ldloc, variables[idx]));
            }

            instructions.Add(Instruction.Create(OpCodes.Callvirt, _methodReferenceProvider.GetInstanceLogMethod(methodReferenceInfo, parameters)));

            _moveNextBody.Replace(oldInstruction, instructions);
        }

        private void ChangeStaticLogCallWithoutParameter(Instruction oldInstruction)
        {
            List<Instruction> instructions = new List<Instruction>();

            MethodReference methodReference = (MethodReference)oldInstruction.Operand;
            MethodReferenceInfo methodReferenceInfo = new MethodReferenceInfo(methodReference);

            instructions.Add(Instruction.Create(OpCodes.Ldsfld, TypeWeaver.CreateLoggerStaticField(_typeReferenceProvider, _methodReferenceProvider, _generatedType)));

            if (!methodReferenceInfo.IsPropertyAccessor())
            {
                instructions.AddRange(LoadMethodNameOnStack());
            }

            instructions.Add(Instruction.Create(OpCodes.Callvirt, _methodReferenceProvider.GetInstanceLogMethod(methodReferenceInfo)));

            _moveNextBody.Replace(oldInstruction, instructions);
        }

        private List<Instruction> CreateReturnValueSavingInstructions(out VariableDefinition returnValueDef)
        {
            //Declare local variable for the return value
            returnValueDef = _moveNextBody.DeclareVariable("$returnValue", _typeReferenceProvider.Object);

            List<Instruction> instructions = new List<Instruction>();
            instructions.Add(Instruction.Create(OpCodes.Dup));
            if (ReturnType.IsGenericParameter)
            {
                //use the generic parameter of the generated state machine
                GenericParameter varType = _generatedType.GenericParameters[0];
                instructions.Add(Instruction.Create(OpCodes.Box, varType));
            }
            else
            {
                if (IsBoxingNeeded(ReturnType))
                {
                    instructions.Add(Instruction.Create(OpCodes.Box, ReturnType));
                }
            }
            instructions.Add(Instruction.Create(OpCodes.Stloc, returnValueDef)); //store return value in local variable

            return instructions;
        }

        private List<Instruction> CreateTraceReturnLoggingInstructions(VariableDefinition returnValueDef)
        {
            List<Instruction> instructions = new List<Instruction>();

            VariableDefinition paramNamesDef = null;
            VariableDefinition paramValuesDef = null;

            bool hasReturnValue = HasReturnValue;

            if (hasReturnValue)
            {
                //Get local variables for the arrays or declare them if they not exist
                paramNamesDef = MoveNextParamNamesVariable;
                paramValuesDef = MoveNextParamValuesVariable;

                //init arrays
                instructions.AddRange(InitArray(paramNamesDef, 1, _typeReferenceProvider.String));
                instructions.AddRange(InitArray(paramValuesDef, 1, _typeReferenceProvider.Object));

                instructions.AddRange(StoreValueReadByInstructionsInArray(paramNamesDef, 0, Instruction.Create(OpCodes.Ldnull)));
                instructions.AddRange(StoreVariableInObjectArray(paramValuesDef, 0, returnValueDef));
            }

            //build up Trace call
            instructions.Add(Instruction.Create(OpCodes.Ldsfld, TypeWeaver.CreateLoggerStaticField(_typeReferenceProvider, _methodReferenceProvider, _generatedType)));
            instructions.AddRange(LoadMethodNameOnStack());

            //start ticks
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(Instruction.Create(OpCodes.Ldfld, _tickFieldRef));
            //end ticks
            instructions.Add(Instruction.Create(OpCodes.Call, _methodReferenceProvider.GetTimestampReference()));

            instructions.Add(hasReturnValue ? Instruction.Create(OpCodes.Ldloc, paramNamesDef) : Instruction.Create(OpCodes.Ldnull));
            instructions.Add(hasReturnValue ? Instruction.Create(OpCodes.Ldloc, paramValuesDef) : Instruction.Create(OpCodes.Ldnull));
            instructions.Add(Instruction.Create(OpCodes.Callvirt, _methodReferenceProvider.GetTraceLeaveReference()));

            return instructions;
        }

        private List<Instruction> CreateTraceReturnWithExceptionLoggingInstructions(VariableDefinition exceptionValue)
        {
            List<Instruction> instructions = new List<Instruction>();

            VariableDefinition paramNamesDef = null;
            VariableDefinition paramValuesDef = null;

            paramNamesDef = MoveNextParamNamesVariable;
            paramValuesDef = MoveNextParamValuesVariable;

            instructions.AddRange(InitArray(paramNamesDef, 1, _typeReferenceProvider.String));
            instructions.AddRange(InitArray(paramValuesDef, 1, _typeReferenceProvider.Object));

            instructions.AddRange(StoreValueReadByInstructionsInArray(paramNamesDef, 0, Instruction.Create(OpCodes.Ldstr, ExceptionMarker)));
            instructions.AddRange(StoreVariableInObjectArray(paramValuesDef, 0, exceptionValue));

            //build up Trace call
            instructions.Add(Instruction.Create(OpCodes.Ldsfld, TypeWeaver.CreateLoggerStaticField(_typeReferenceProvider, _methodReferenceProvider, _generatedType)));
            instructions.AddRange(LoadMethodNameOnStack());

            //start ticks
            instructions.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructions.Add(Instruction.Create(OpCodes.Ldfld, _tickFieldRef));
            //end ticks
            instructions.Add(Instruction.Create(OpCodes.Call, _methodReferenceProvider.GetTimestampReference()));

            instructions.Add(Instruction.Create(OpCodes.Ldloc, paramNamesDef));
            instructions.Add(Instruction.Create(OpCodes.Ldloc, paramValuesDef));
            instructions.Add(Instruction.Create(OpCodes.Callvirt, _methodReferenceProvider.GetTraceLeaveReference()));

            return instructions;
        }

        private VariableDefinition _moveNextParamNamesVariable;

        protected VariableDefinition MoveNextParamNamesVariable
        {
            get
            {
                if (_moveNextParamNamesVariable == null)
                {
                    _moveNextParamNamesVariable = _moveNextBody.DeclareVariable("$paramNames", _typeReferenceProvider.StringArray);
                }
                return _moveNextParamNamesVariable;
            }
        }

        private VariableDefinition _moveNextParamValuesVariable;

        protected VariableDefinition MoveNextParamValuesVariable
        {
            get
            {
                if (_moveNextParamValuesVariable == null)
                {
                    _moveNextParamValuesVariable = _moveNextBody.DeclareVariable("$paramValues", _typeReferenceProvider.ObjectArray);
                }
                return _moveNextParamValuesVariable;
            }
        }

        private bool IsCallSetResult(Instruction instr)
        {
            if (instr.OpCode != OpCodes.Call) return false;
            MethodReference methodRef = instr.Operand as MethodReference;
            if (methodRef == null) return false;

            return (methodRef.Name.Equals("SetResult", StringComparison.OrdinalIgnoreCase) &&
            (methodRef.DeclaringType.FullName.StartsWith("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", StringComparison.OrdinalIgnoreCase)));
        }

        private bool IsCallSetException(Instruction instr)
        {
            if (instr.OpCode != OpCodes.Call) return false;
            MethodReference methodRef = instr.Operand as MethodReference;
            if (methodRef == null) return false;

            return (methodRef.Name.Equals("SetException", StringComparison.OrdinalIgnoreCase) &&
            (methodRef.DeclaringType.FullName.StartsWith("System.Runtime.CompilerServices.AsyncTaskMethodBuilder", StringComparison.OrdinalIgnoreCase)));
        }

        protected override TypeReference ReturnType
        {
            get
            {
                if (base.ReturnType.FullName.Equals(_typeReferenceProvider.Task.FullName)) return _typeReferenceProvider.Void;
                GenericInstanceType type = base.ReturnType as GenericInstanceType;
                if (type != null && type.GenericArguments.Count == 1)
                {
                    return type.GenericArguments[0];
                }
                return base.ReturnType;
            }
        }


        protected override void SearchForAndReplaceStaticLogCalls()
        {

        }

        private void ExtendGeneratedTypeWithTickcountField()
        {
            FieldDefinition tickField = new FieldDefinition(StartTickVarName, FieldAttributes.Public, _typeReferenceProvider.Long);
            _generatedType.Fields.Add(tickField);
            _tickFieldRef = tickField;
        }
    }
}
