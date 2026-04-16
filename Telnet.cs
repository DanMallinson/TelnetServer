namespace TelnetServer
{
    internal class Telnet
    {
        public const byte IAC = 255;
        public const byte DONT = 254;
        public const byte DO = 253;
        public const byte WONT = 252;
        public const byte WILL = 251;
        public const byte SB = 250;
        public const byte SE = 240;

        public const byte OPT_ECHO = 1;
        public const byte OPT_SUPPRESS_GO_AHEAD = 3;
        public const byte OPT_NAWS = 31;
        public const byte OPT_TERMINAL_TYPE = 24;
    }
}
