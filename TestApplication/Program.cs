using System;
using System.IO;

namespace TestApplication
{
    public class Program
    {
        public static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure(new FileInfo("Logging.config"));
            Console.WriteLine("Starting application...");
            MyApplication myApp = new MyApplication();
            myApp.Run();
            Console.WriteLine("Press Enter to stop");
            Console.ReadLine();
        }
    }
}
