﻿using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Tracer.Fody.Weavers
{
    /// <summary>
    /// Porvides types used during weaving bound to the given module definition.
    /// </summary>
    internal class TypeReferenceProvider
    {
        private readonly Lazy<TypeReference> _stringArray;
        private readonly Lazy<TypeReference> _objectArray;
        private readonly Lazy<TypeReference> _type;
        private readonly Lazy<TypeReference> _stopwatch;
        private readonly Lazy<TypeReference> _exception;
        private readonly Lazy<TypeReference> _asyncStateMachineAttribute;
        private readonly Lazy<TypeReference> _task;
        private readonly ModuleDefinition _moduleDefinition;
        private readonly TraceLoggingConfiguration _configuration;
        private readonly ILoggerAdapterMetadataScopeProvider _loggerAdapterMetadataScopeProvider;

        public TypeReferenceProvider(TraceLoggingConfiguration configuration, ILoggerAdapterMetadataScopeProvider loggerAdapterMetadataScopeProvider, ModuleDefinition moduleDefinition)
        {
            _configuration = configuration;
            _moduleDefinition = moduleDefinition;
            _loggerAdapterMetadataScopeProvider = loggerAdapterMetadataScopeProvider;
            _stringArray = new Lazy<TypeReference>(() => moduleDefinition.ImportReference((typeof(string[]))));
            _objectArray = new Lazy<TypeReference>(() => moduleDefinition.ImportReference(typeof(object[])));
            _type = new Lazy<TypeReference>(() => moduleDefinition.ImportReference(typeof(Type)));
            _stopwatch = new Lazy<TypeReference>(() => moduleDefinition.ImportReference(typeof(Stopwatch)));
            _exception = new Lazy<TypeReference>(() => moduleDefinition.ImportReference(typeof(Exception)));
            _asyncStateMachineAttribute = new Lazy<TypeReference>(() => moduleDefinition.ImportReference(
                typeof(System.Runtime.CompilerServices.AsyncStateMachineAttribute)));
            _task = new Lazy<TypeReference>(() => moduleDefinition.ImportReference(typeof(Task)));
        }

        public TypeReference StringArray
        {
            get { return _stringArray.Value; }
        }

        public TypeReference ObjectArray
        {
            get { return _objectArray.Value; }
        }

        public TypeReference Type
        {
            get { return _type.Value; }
        }

        public TypeReference String
        {
            get { return _moduleDefinition.TypeSystem.String; }
        }
        public TypeReference Object
        {
            get { return _moduleDefinition.TypeSystem.Object; }
        }

        public TypeReference AsyncStateMachineAttribute
        {
            get { return _asyncStateMachineAttribute.Value; }
        }

        public TypeReference Exception
        {
            get { return _exception.Value; }
        }

        public TypeReference Void
        {
            get { return _moduleDefinition.TypeSystem.Void; }
        }

        public TypeReference Long
        {
            get { return _moduleDefinition.TypeSystem.Int64; }
        }

        public TypeReference Task
        {
            get { return _task.Value; }
        }

        public TypeReference LogAdapterReference
        {
            get
            {
                IMetadataScope loggerScope = _loggerAdapterMetadataScopeProvider.GetLoggerAdapterMetadataScope();
                TraceLoggingConfiguration.TypeName logger = _configuration.Logger; 
                return new TypeReference(logger.Namespace, logger.Name, _moduleDefinition, loggerScope); 
            }
        }

        public TypeReference StaticLogReference
        {
            get
            {
                IMetadataScope loggerScope = _loggerAdapterMetadataScopeProvider.GetLoggerAdapterMetadataScope();
                TraceLoggingConfiguration.TypeName staticLogger = _configuration.StaticLogger;
                return new TypeReference(staticLogger.Namespace, staticLogger.Name, _moduleDefinition, loggerScope); 
            }
        }

        public TypeReference LogManagerReference
        {
            get
            {
                IMetadataScope loggerScope = _loggerAdapterMetadataScopeProvider.GetLoggerAdapterMetadataScope();
                TraceLoggingConfiguration.TypeName logManager = _configuration.LogMannager;
                return new TypeReference(logManager.Namespace, logManager.Name, _moduleDefinition, loggerScope);
            }
        }

        public TypeReference Stopwatch
        {
            get { return _stopwatch.Value; }
        }

    }
}
