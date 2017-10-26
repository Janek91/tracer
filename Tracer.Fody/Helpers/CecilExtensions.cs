using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Tracer.Fody.Helpers
{
    /// <summary>
    /// Helpers for Cecil
    /// </summary>
    public static class CecilExtensions
    {
        /// <summary>
        /// Inserts the given instructions before the current (this) instruction using the given processor
        /// </summary>
        public static void InsertBefore(this Instruction instruction, ILProcessor processor, IEnumerable<Instruction> instructions)
        {
            foreach (Instruction newInstruction in instructions)
            {
                processor.InsertBefore(instruction, newInstruction);
            }
        }

        /// <summary>
        /// Inserts the given instructions after the current (this) instruction using the given processor
        /// </summary>
        public static void InsertAfter(this Instruction instruction, ILProcessor processor, IEnumerable<Instruction> instructions)
        {
            foreach (Instruction newInstruction in instructions.Reverse())
            {
                processor.InsertAfter(instruction, newInstruction);
            }
        }

        /// <summary>
        /// Inserts the given instructions at the beginning of the method body keeping the debug sequence point intact
        /// </summary>
        public static void InsertAtTheBeginning(this MethodBody body, IEnumerable<Instruction> instructions)
        {
            MethodDebugInformation debugInfo = body.Method?.DebugInformation;
            ILProcessor processor = body.GetILProcessor();
            if (body.Instructions.Count > 0)
            {
                SequencePoint seqPointToReplace = debugInfo?.GetSequencePoint(body.Instructions[0]);
                body.Instructions[0].InsertBefore(processor, instructions);

                if (seqPointToReplace != null)
                {
                    SequencePoint newSeqPoint = new SequencePoint(body.Instructions[0], seqPointToReplace.Document)
                    {
                        StartColumn = seqPointToReplace.StartColumn,
                        EndColumn = seqPointToReplace.EndColumn,
                        StartLine = seqPointToReplace.StartLine,
                        EndLine = seqPointToReplace.EndLine
                    };
                    int idx = debugInfo.SequencePoints.IndexOf(seqPointToReplace);
                    debugInfo.SequencePoints[idx] = newSeqPoint;
                }

            }
            else
            {
                foreach (Instruction instruction in instructions)
                {
                    processor.Append(instruction);
                }
            }
        }

        /// <summary>
        /// Inserts the given instructions at the end of the method body keeping the debug sequence point intact, returning the first 
        /// </summary>
        /// <returns>
        /// The first isntruction added
        /// </returns>
        public static Instruction AddAtTheEnd(this MethodBody body, IEnumerable<Instruction> instructions)
        {
            foreach (Instruction instruction in instructions)
            {
                body.Instructions.Add(instruction);
            }
            return instructions.First();
        }

        public static void Replace(this MethodBody body, Instruction instructionToReplace, ICollection<Instruction> newInstructions)
        {
            Replace(body.Instructions, instructionToReplace, newInstructions);
        }

        /// <summary>
        /// Replaces the given instruction in the collection of instructions with the new instructions
        /// </summary>
        public static void Replace(this Collection<Instruction> collection, Instruction instructionToReplace, ICollection<Instruction> newInstructions)
        {
            Instruction newInstruction = newInstructions.First();
            instructionToReplace.Operand = newInstruction.Operand;
            instructionToReplace.OpCode = newInstruction.OpCode;

            int indexOf = collection.IndexOf(instructionToReplace);
            foreach (Instruction instruction1 in newInstructions.Skip(1))
            {
                collection.Insert(indexOf + 1, instruction1);
                indexOf++;
            }
        }


        public static TypeReference GetGenericInstantiationIfGeneric(this TypeReference definition)
        {
            if (!definition.HasGenericParameters) return definition;
            GenericInstanceType instType = new GenericInstanceType(definition);
            foreach (GenericParameter parameter in definition.GenericParameters)
            {
                instType.GenericArguments.Add(parameter);
            }
            return instType;
        }

        public static FieldReference FixFieldReferenceIfDeclaringTypeIsGeneric(this FieldReference fieldReference)
        {
            if (fieldReference.DeclaringType.HasGenericParameters)
            {
                return new FieldReference(fieldReference.Name, fieldReference.FieldType, fieldReference.DeclaringType.GetGenericInstantiationIfGeneric());
            }
            return fieldReference;
        }

        public static VariableDefinition DeclareVariable(this MethodBody body, string name, TypeReference type)
        {
            VariableDefinition variable = new VariableDefinition(type);
            body.Variables.Add(variable);
            VariableDebugInformation variableDebug = new VariableDebugInformation(variable, name);
            body.Method?.DebugInformation?.Scope?.Variables?.Add(variableDebug);
            return variable;
        }

        public static bool IsPropertyAccessor(this MethodDefinition methodDefinition)
        {
            return (methodDefinition.IsGetter || methodDefinition.IsSetter);
        }
    }
}
