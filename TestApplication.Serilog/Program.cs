using System;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Elasticsearch;

namespace TestApplication.Serilog
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.RollingFile("c:\\ApplicationLogs\\log-{Date}.txt",
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{MachineName}][{ThreadId}][{Level}]{ClassName}.{MethodName} {Message}{NewLine}{Exception}")
                .WriteTo.Elasticsearch(new ElasticsearchSinkOptions(new Uri("http://localhost:9200"))
                {
                    AutoRegisterTemplate = true,
                    MinimumLogEventLevel = LogEventLevel.Debug
                })
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .CreateLogger();
            Console.WriteLine("Starting application...");

            MyApplication myApp = new MyApplication();
            myApp.Run();
            Console.WriteLine("Press Enter to stop");
            Console.ReadLine();
        }
    }
}
