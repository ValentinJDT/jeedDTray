using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace jeeDTray
{
    internal class Logger
    {
        private static string file_name = Configuration.CONFIG_DIR + "logs.txt";

        public static void Info(string message)
        {
            DateTime now = DateTime.Now;
            File.AppendAllLines(file_name, new string[] { "[" + now.ToString("MM-dd-yyyy HH:mm:ss") + "] Info  - " + message });
        }

        public static void Error(string message)
        {
            DateTime now = DateTime.Now;
            File.AppendAllLines(file_name, new string[] { "[" + now.ToString("MM-dd-yyyy HH:mm:ss") + "] Error - " + message });
        }
    }
}
