using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetServer
{
    internal class NewsMenu : Menu
    {

        public List<Tuple<string,string>> Sources {  get; set; } = new List<Tuple<string,string>>();

        private int _offset = 0;
        public override string GetContent(int width, int height)
        {
            var builder = new StringBuilder();

            builder.Append("".PadLeft(width, '=')+"\r\n");
            for(var i = 0; i< height; ++i)
            {
                var idx = _offset + i;
                if (idx < Sources.Count)
                {
                    builder.Append($"{i}.\t{Sources[idx].Item1}\r\n");
                }
                else
                {
                    break;
                }
            }
            builder.Append("".PadLeft(width, '='));
            builder.Append("\r\n");
            return builder.ToString();
        }
        protected override void ProcessCommandInternal(string command, TelnetClient client)
        {
            var idx = -1;

            if (int.TryParse(command, out idx) && idx >=0 && idx < Sources.Count)
            {
                var newMenu = new HeadlineMenu()
                {
                    Title = Sources[idx].Item1,
                    Feed = Sources[idx].Item2,
                };

                client.SetMenu(newMenu,true);
            }
        }
    }
}
