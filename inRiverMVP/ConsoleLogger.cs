using inRiver.Remoting.Extension;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using inRiver.Remoting.Log;

namespace Aprimo.InRiver
{
    internal class ConsoleLogger : IExtensionLog
    {
        public void Log(LogLevel level, string message)
        {
            Console.WriteLine($"{level} - {message}");
        }

        public void Log(LogLevel level, string message, Exception ex)
        {
            Console.WriteLine($"{level} - {message}");
            Console.WriteLine(ex.ToString());
        }
    }
}