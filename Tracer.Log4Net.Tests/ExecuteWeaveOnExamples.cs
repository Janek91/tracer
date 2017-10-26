using Mono.Cecil;
using NUnit.Framework;
using Tracer.Fody;
using Tracer.Fody.Weavers;


namespace Tracer.Log4Net.Tests
{
    [TestFixture]
    public class ExecuteWeaveOnExamples
    {
        [Test, Explicit, Category("manual")]
        public void WeaveMyApplication()
        {
            TraceLoggingConfiguration.TraceLoggingConfigurationBuilder config = TraceLoggingConfiguration.New
                .WithFilter(new PublicMethodsFilter())
                .WithAdapterAssembly(typeof(Log).Assembly.GetName().FullName)
                .WithLogManager(typeof(Adapters.LogManagerAdapter).FullName)
                .WithLogger(typeof(Adapters.LoggerAdapter).FullName)
                .WithStaticLogger(typeof(Log).FullName);

            AssemblyWeaver.Execute("..\\..\\..\\TestApplication\\bin\\debug\\TestApplication.exe", config);
        }

        private class PublicMethodsFilter : ITraceLoggingFilter
        {
            public bool ShouldAddTrace(MethodDefinition definition)
            {
                return true;
            }
        }
    }
}
