using NLog;
using NLog.Config;
using NLog.Targets;

namespace TestApplication.NLog
{
    public class Program
    {
        public static void Main(string[] args)
        {
            LoggingConfiguration config = new LoggingConfiguration();

            FileTarget fileTarget = new FileTarget();
            fileTarget.FileName = "c:\\ApplicationLogs\\log_nlog.txt";
            fileTarget.Layout = "${time}${logger}[${threadid}][${level}] ${event-properties:item=TypeInfo}.${event-properties:item=MethodInfo} - ${message}";

            config.AddTarget("file", fileTarget);
            config.AddRuleForAllLevels(fileTarget);

            LogManager.Configuration = config;

            MyApplication app = new MyApplication();
            app.Run();
        }
    }
}
