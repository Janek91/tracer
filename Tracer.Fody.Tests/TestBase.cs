#define NO_RANDOM_FOLDERS

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CSharp;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using NUnit.Framework;
using Tracer.Fody.Tests.MockLoggers;
using Tracer.Fody.Weavers;

namespace Tracer.Fody.Tests
{
    /// <summary>
    /// Class containing helper methods for functional tests
    /// </summary>
    public class TestBase
    {
        private const string CustomTempFolder = @"c:\temp";

        private string _tempFolder;

        [SetUp]
        public void Setup()
        {
            string tempFolderBase = Directory.Exists(CustomTempFolder) ? CustomTempFolder : Path.GetTempPath();

#if NO_RANDOM_FOLDERS
            _tempFolder = tempFolderBase;
#else
            _tempFolder = Path.Combine(tempFolderBase, Path.GetRandomFileName());
#endif

            Directory.CreateDirectory(_tempFolder);
        }

        [TearDown]
        public void TearDown()
        {
#if !NO_RANDOM_FOLDERS  //keep files if no random folders is set
            Directory.Delete(_tempFolder, true);
#endif
        }

        private string GetDestinationFilePath(string assemblyName)
        {

            return Path.Combine(_tempFolder, Path.ChangeExtension(assemblyName, ".dll"));
        }

        /// <summary>
        /// Complies the give source and returns the resulting assembly's full path
        /// </summary>
        protected string Compile(string source, string assemblyName, string[] additonalAssemblies)
        {
            string destPath = GetDestinationFilePath(assemblyName);

            using (CSharpCodeProvider provider = new CSharpCodeProvider())
            {
                CompilerParameters parameters = new CompilerParameters { OutputAssembly = destPath, IncludeDebugInformation = true };

                parameters.ReferencedAssemblies.Add("System.dll");
                parameters.ReferencedAssemblies.Add("System.Core.dll");
                parameters.ReferencedAssemblies.Add("System.Data.dll");
                if (additonalAssemblies != null)
                    parameters.ReferencedAssemblies.AddRange(additonalAssemblies);

                CompilerResults results = provider.CompileAssemblyFromSource(parameters, source);

                if (results.Errors.HasErrors)
                {
                    StringBuilder sb = new StringBuilder();

                    foreach (CompilerError error in results.Errors)
                    {
                        sb.AppendLine(string.Format("Error ({0}): {1}", error.ErrorNumber, error.ErrorText));
                    }

                    throw new InvalidOperationException(sb.ToString());
                }

                Debug.Write(string.Format("Dll compiled to {0}", destPath));

                return destPath;
            }
        }

        protected void Rewrite(string assemblyPath, ITraceLoggingFilter filter, bool traceConstructors = false)
        {
            //Set-up log adapter to our mock 
            Assembly assembly = Assembly.GetExecutingAssembly();

            TraceLoggingConfiguration.TraceLoggingConfigurationBuilder config = TraceLoggingConfiguration.New
                .WithFilter(filter)
                .WithAdapterAssembly(assembly.GetName().FullName)
                .WithLogManager(typeof(MockLogManagerAdapter).FullName)
                .WithLogger(typeof(MockLogAdapter).FullName)
                .WithStaticLogger(typeof(MockLog).FullName);

            if (traceConstructors) config.WithConstructorTraceOn();

            AssemblyWeaver.Execute(assemblyPath, config);
        }

        protected MockLogResult RunTest(string source, ITraceLoggingFilter filter, string staticEntryPoint, bool shouldTraceConstructors = false)
        {
            string[] splitEntry = staticEntryPoint.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);
            if (splitEntry.Length != 2) throw new ApplicationException("Static entry point must be in a form Namesp.Namesp2.Class::Method");
            string entryClass = splitEntry[0];
            string entryMethod = splitEntry[1];

            Uri testDllLocation = new Uri(Assembly.GetExecutingAssembly().CodeBase);

            string assemblyPath = Compile(source, "testasm", new[] { testDllLocation.AbsolutePath });
            Rewrite(assemblyPath, filter, shouldTraceConstructors);

            //----
            return RunCode(assemblyPath, entryClass, entryMethod);
        }

        protected MockLogResult RunCode(string assemblyPath, string entryClass, string entryMethod)
        {
            AppDomainSetup currentSetup = AppDomain.CurrentDomain.SetupInformation;
            AppDomain appDomain = AppDomain.CreateDomain("testrun", null, currentSetup);
            try
            {
                Worker remote = (Worker)appDomain.CreateInstanceAndUnwrap(Assembly.GetExecutingAssembly().FullName, "Tracer.Fody.Tests.TestBase+Worker");
                MockLogResult result = remote.Run(assemblyPath, entryClass, entryMethod, typeof(MockLogManagerAdapter).FullName);
                return result;
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }

        protected MethodDefinition GetMethodDefinition(string source, string methodName)
        {
            Uri testDllLocation = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            string assemblyPath = Compile(source, "testasm", new[] { testDllLocation.AbsolutePath });

            using (ModuleDefinition moduleDef = ModuleDefinition.ReadModule(assemblyPath))
            {
                return moduleDef.GetAllTypes().SelectMany(typeDef => typeDef.Methods)
                    .FirstOrDefault(methodDef => methodDef.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));
            }
        }

        //This is the bridge between the two appdomains
        private class Worker : MarshalByRefObject
        {
            public MockLogResult Run(string assemblyPath, string mainClass, string mainMethod, string logManagerTypeName)
            {
                Assembly asm = Assembly.LoadFile(assemblyPath);
                Type type = asm.GetType(mainClass);
                MethodInfo mainMethodInfo = type.GetMethod(mainMethod, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                int numberOfParameters = mainMethodInfo.GetParameters().Length;
                mainMethodInfo.Invoke(null, new object[numberOfParameters]);
                Type logManagerType = Type.GetType(logManagerTypeName);
                MethodInfo getMockResultMethod = logManagerType.GetMethod("GetResult", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                return (MockLogResult)getMockResultMethod.Invoke(null, new object[0]);
            }

        }

        #region Various trace logging filters for testing

        protected class AllTraceLoggingFilter : ITraceLoggingFilter
        {
            public bool ShouldAddTrace(MethodDefinition definition)
            {
                return true;
            }
        }

        protected class NoTraceLoggingFilter : ITraceLoggingFilter
        {
            public bool ShouldAddTrace(MethodDefinition definition)
            {
                return false;
            }
        }

        protected class PrivateOnlyTraceLoggingFilter : ITraceLoggingFilter
        {
            public bool ShouldAddTrace(MethodDefinition definition)
            {
                return definition.IsPrivate;
            }
        }

        protected class InternalOnlyTraceLoggingFilter : ITraceLoggingFilter
        {
            public bool ShouldAddTrace(MethodDefinition definition)
            {
                return definition.IsAssembly;
            }
        }

        #endregion
    }
}
