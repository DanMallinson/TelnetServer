using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TelnetServer
{
    internal class ConfigMenu : Menu
    {
        public enum eMode
        {
            Terminal,
            Width,
            Height,
        };

        public eMode Mode {  get; set; }
        public ConfigMenu() { }

        public override string GetContent(int width, int height)
        {
            switch(Mode)
            {
                case eMode.Terminal:
                    {
                        var values = Enum.GetNames(typeof(TelnetClient.eTerminalType));

                        var builder = new StringBuilder();
                        builder.AppendLine();
                        builder.AppendLine("Select Terminal Type");
                        for (var i =0; i < values.Length; ++i)
                        {
                            builder.AppendLine($"{i} : {values[i]}");
                        }
                        return builder.ToString();
                    }
                    break;
                case eMode.Width:
                    return "\nEnter Terminal Width:";
                case eMode.Height:
                    return "\nEnter Terminal Height:";
            }

            return base.GetContent(width, height);
        }

        protected override void ProcessCommandInternal(string command, TelnetClient client)
        {
            if (int.TryParse(command, out int value))
            {
                switch (Mode)
                {
                    case eMode.Terminal:
                        {
                            var values = Enum.GetNames(typeof(TelnetClient.eTerminalType));
                            if (value > 0 && value < values.Length)
                            {
                                client.TerminalType = (TelnetClient.eTerminalType)value;
                                Mode = eMode.Width;
                            }
                            client.SetMenu(this, false);
                        }
                        break;

                    case eMode.Width:
                        if (value > 0)
                        {
                            client.Width = value;
                            Mode = eMode.Height;
                        }
                        client.SetMenu(this, false);
                        break;
                    case eMode.Height:
                        if (value > 0)
                        {
                            client.Height = value;
                            client.SetMenu("default", true);
                        }
                        Mode = eMode.Terminal;
                        break;
                }
            }
            else
            {
                client.SetMenu(this, false);
            }
        }
    }
}
