using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetServer
{
    internal class Menu
    {
        public string Content { get; set; } = string.Empty;
        public SortedDictionary<string,Func<MenuResponse>> Actions { get; set; }

        public Menu() 
        {
            Actions = new SortedDictionary<string, Func<MenuResponse>>();
        }

        public void Initialise()
        {
            OnInitialise();
        }

        public virtual string GetContent(int width, int height)
        {
            var builder = new StringBuilder();

            builder.AppendLine("".PadLeft(width, '='));
            builder.AppendLine(Content);
            builder.AppendLine("".PadLeft(width, '='));

            return builder.ToString();
        }

        public void ProcessCommand(string command, TelnetClient client)
        {
            if (command == null || command.Length == 0)
            {
                client.SetMenu(this,false);
            }
            else if (command == "O")
            {
                client.Disconnect();
            }
            else if(command == "\\")
            {
                client.PopMenu();
            }
            else
            {
                ProcessCommandInternal(command, client);
            }
        }

        protected virtual void ProcessCommandInternal(string command, TelnetClient client)
        {
        }

        protected virtual void OnInitialise()
        {

        }
    }

    internal class MenuResponse
    {
        public string Value { get; set; } = string.Empty;
        public bool IsMenu { get; set; } = false;
    }
    
}
