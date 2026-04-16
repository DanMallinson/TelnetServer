using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetServer
{
    static internal class Defines
    {
        public const string ClearCommand = "\x1b[2J\x1b[H";

        public static string GetHeading(string text, int width)
        {
            if (text.Length >= width)
            {
                return text;
            }

            var remaining = width - text.Length;
            var left = remaining / 2;
            var right = remaining - left;
            var result = new string('=', left) + text + new string('=', right);

            bool front = false;
            while (result.Length >= width)
            {
                if (front)
                {
                    result = result.Substring(1);
                }
                else
                {
                    result = result.Substring(0, result.Length - 1);
                }
                front = !front;
            }

            return result;
        }
    }
}
