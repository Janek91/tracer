using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Mono.Cecil;

namespace Tracer.Fody.Weavers
{
    internal class MethodReferenceProvider
    {
        private readonly ModuleDefinition _moduleDefinition;
        private readonly TypeReferenceProvider _typeReferenceProvider;

        public MethodReferenceProvider(TypeReferenceProvider typeReferenceProvider, ModuleDefinition moduleDefinition)
        {
            _moduleDefinition = moduleDefinition;
            _typeReferenceProvider = typeReferenceProvider;
        }

        public MethodReference GetTraceEnterReference()
        {
            MethodReference logTraceEnterMethod = new MethodReference("TraceEnter", _moduleDefinition.TypeSystem.Void, _typeReferenceProvider.LogAdapterReference);
            logTraceEnterMethod.HasThis = true; //instance method
            logTraceEnterMethod.Parameters.Add(new ParameterDefinition(_moduleDefinition.TypeSystem.String));
            logTraceEnterMethod.Parameters.Add(new ParameterDefinition(_typeReferenceProvider.StringArray));
            logTraceEnterMethod.Parameters.Add(new ParameterDefinition(_typeReferenceProvider.ObjectArray));
            return logTraceEnterMethod;
        }

        public MethodReference GetTraceLeaveReference()
        {
            MethodReference logTraceLeaveMethod = new MethodReference("TraceLeave", _moduleDefinition.TypeSystem.Void, _typeReferenceProvider.LogAdapterReference);
            logTraceLeaveMethod.HasThis = true; //instance method
            logTraceLeaveMethod.Parameters.Add(new ParameterDefinition(_moduleDefinition.TypeSystem.String));
            logTraceLeaveMethod.Parameters.Add(new ParameterDefinition(_moduleDefinition.TypeSystem.Int64));
            logTraceLeaveMethod.Parameters.Add(new ParameterDefinition(_moduleDefinition.TypeSystem.Int64));
            logTraceLeaveMethod.Parameters.Add(new ParameterDefinition(_typeReferenceProvider.StringArray));
            logTraceLeaveMethod.Parameters.Add(new ParameterDefinition(_typeReferenceProvider.ObjectArray));
            return logTraceLeaveMethod;
        }

        public MethodReference GetGetTypeFromHandleReference()
        {
            return _moduleDefinition.ImportReference(typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Public | BindingFlags.Static));
        }

        public MethodReference GetTimestampReference()
        {
            return _moduleDefinition.ImportReference(typeof(Stopwatch).GetMethod("GetTimestamp", BindingFlags.Public | BindingFlags.Static));
        }

        public MethodReference GetInstanceLogMethod(MethodReferenceInfo methodReferenceInfo, IEnumerable<ParameterDefinition> parameters = null)
        {
            parameters = parameters ?? new ParameterDefinition[0];

            MethodReference logMethod = new MethodReference(GetInstanceLogMethodName(methodReferenceInfo), methodReferenceInfo.ReturnType, _typeReferenceProvider.LogAdapterReference);
            logMethod.HasThis = true; //instance method

            //check if accessor
            if (!methodReferenceInfo.IsPropertyAccessor())
            {
                logMethod.Parameters.Add(new ParameterDefinition(_moduleDefinition.TypeSystem.String));
            }

            foreach (ParameterDefinition parameter in parameters)
            {
                logMethod.Parameters.Add(parameter);
            }

            //handle generics
            if (methodReferenceInfo.IsGeneric)
            {
                foreach (GenericParameter genericParameter in methodReferenceInfo.GenericParameters)
                {
                    GenericParameter gp = new GenericParameter(genericParameter.Name, logMethod);
                    gp.Name = genericParameter.Name;
                    logMethod.GenericParameters.Add(gp);
                }
                logMethod.CallingConvention = MethodCallingConvention.Generic;

                logMethod = new GenericInstanceMethod(logMethod);
                foreach (TypeReference genericArgument in methodReferenceInfo.GenericArguments)
                {
                    ((GenericInstanceMethod)logMethod).GenericArguments.Add(genericArgument);
                }
            }

            return logMethod;
        }

        private string GetInstanceLogMethodName(MethodReferenceInfo methodReferenceInfo)
        {
            //TODO chain inner types in name
            string typeName = methodReferenceInfo.DeclaringType.Name;

            if (methodReferenceInfo.IsPropertyAccessor())
            {
                return "get_" + typeName + methodReferenceInfo.Name.Substring(4);
            }
            else
            {
                return typeName + methodReferenceInfo.Name;
            }
        }

    }
}
