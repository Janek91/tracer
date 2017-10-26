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
                .WithAdapterAssembly(typeof(Serilog.Log).Assembly.GetName().FullName)
                .WithLogManager(typeof(Serilog.Adapters.LogManagerAdapter).FullName)
                .WithLogger(typeof(Serilog.Adapters.LoggerAdapter).FullName)
                .WithStaticLogger(typeof(Serilog.Log).FullName);

            AssemblyWeaver.Execute("..\\..\\..\\TestApplication.Serilog\\bin\\debug\\TestApplication.Serilog.exe", config);
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
