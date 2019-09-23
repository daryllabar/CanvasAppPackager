using System;
using System.Collections.Generic;
using System.Text;

namespace CanvasAppPackager
{
    class Logger
    {
        public static void Log(string log)
        {
            Console.WriteLine(log);
        }

        public static void Error(string error)
        {
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(error);
            Console.ForegroundColor = color;
        }
    }
}
