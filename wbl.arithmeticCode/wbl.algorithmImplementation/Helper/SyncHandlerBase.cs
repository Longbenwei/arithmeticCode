using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace wbl.algorithmImplementation.Helper
{
    public static class SyncHandlerBase
    {
        //public static ILog Logger = new Beisen.Logging.LogWrapper();

        public static void DebugLog(string msg, bool isLine = true, ConsoleColor color = ConsoleColor.Yellow, Exception ex = null)
        {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            if (isLine)
                Console.WriteLine(msg);
            else
                Console.Write(msg);

            Console.ForegroundColor = currentColor;

            //if (ex != null)
            //    Logger.Debug(msg, ex);
            //else
            //    Logger.Debug(msg);
        }

        internal static void ErrorLog(string msg, Exception ex)
        {
            ConsoleColor currentColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(msg);
            Console.ForegroundColor = currentColor;

            //Logger.Error(msg, ex);
        }

        //internal static int GetProcessCount()
        //{
        //    string processCountString = ConfigurationManager.AppSettings["processorCount"];
        //    int processCount = Environment.ProcessorCount;
        //    if (!string.IsNullOrEmpty(processCountString))
        //        processCount = int.Parse(processCountString);
        //    return processCount;
        //}
    }
}
