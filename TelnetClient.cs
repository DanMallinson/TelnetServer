using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TelnetServer
{
    internal class TelnetClient
    {
        public enum eTerminalType
        {
            Modern,
            Vt52,
            Dumb,
        };

        const int PacketSize = 16;
        public TcpClient TcpClient { get ; set; }
        public Menu CurrentMenu { get; set; }
        
        public SortedDictionary<string,Menu> Menus { get; set; }
        public int Width { get; set; } = 80;
        public int Height { get; set; } = 10;

        private string _buffer;
        public eTerminalType TerminalType { get; set; }

        private Stack<Menu> _menuStack = new Stack<Menu>();

        private bool
            _clientEcho,
            _serverEcho,
            _fullDuplex,
            _lastWasCR;

        public bool IsDisconnected()
        {
            return TcpClient == null;
        }

        public void OnInitialise()
        {
            DetectTerminalType();
            var configMenu = new ConfigMenu();
            _menuStack.Push(configMenu);
            if (DetectWidth())
            {
                SetMenu("default", false);
            }
            else
            {
                SetMenu(configMenu, false);
            }
        }

        public void Update()
        {
            if(TcpClient == null)
            {
                return;
            }

            ReadInput();
            //Todo - work out if we auto populate or not
        }

        private void DetectTerminalType()
        {
            var stream = TcpClient.GetStream();
            if(ProbeTelnet(stream))
            {
                TerminalType = eTerminalType.Modern;
            }
            else if (ProbeAnsi(stream))
            {
                TerminalType = eTerminalType.Modern;
            }
            else if (ProbeVt52(stream))
            {
                TerminalType = eTerminalType.Vt52;
            }
            else
            {
                TerminalType = eTerminalType.Dumb;
            }
        }

        private bool ProbeTelnet(Stream stream)
        {
            var probe = new byte[] { Telnet.IAC, Telnet.DO, 1 };
            stream.Write(probe, 0, probe.Length);
            var response = ReadWithTimeout(stream, 150);

            if (response.Length == 0)
            {
                return false;
            }


            if(response.SequenceEqual(probe))
            {
                return false;
            }

            if(response.Length == 3 &&
                response[0] == 255 &&
                (response[1] == 251 || response[1] == 252))
            {
                return true;
            }

            return false;
        }

        private bool ProbeAnsi(Stream stream)
        {
            var probe = Encoding.ASCII.GetBytes("\x1B[6n");
            stream.Write(probe, 0, probe.Length);
            var response = ReadWithTimeout(stream, 150);

            if (response.Length == 0)
            {
                return false;
            }

            var s = Encoding.ASCII.GetString(response);

            return s.Contains("\x1B[") && s.EndsWith("R");
        }

        private bool ProbeVt52(Stream stream)
        {
            var probe = Encoding.ASCII.GetBytes("X");
            stream.Write(probe,0, probe.Length);
            probe = Encoding.ASCII.GetBytes("\x1BA");
            stream.Write(probe, 0, probe.Length);
            probe = Encoding.ASCII.GetBytes("Y");
            stream.Write(probe, 0, probe.Length);
            var response = ReadWithTimeout(stream, 150);
            if (response.Length == 0)
            {
                return false;
            }

            var s = Encoding.ASCII.GetString(response);

            if(s.Contains("\x1BA"))
            {
                return false;
            }

            return s.Contains("X") && s.Contains("Y");
        }

        private byte[] ReadWithTimeout(Stream stream, int timeout)
        {
            var buffer = new byte[256];
            var cts = new CancellationToken();

            try
            {
                var currentTimeout = stream.ReadTimeout;
                stream.ReadTimeout = timeout;
                var read = stream.Read(buffer, 0, buffer.Length);
                stream.ReadTimeout = currentTimeout;
                if(read > 0)
                {
                    var result = new byte[read];
                    Array.Copy(buffer, result, read);
                    return result;
                }
            }
            catch { }

            return Array.Empty<byte>();
        }

        private bool DetectWidth()
        {
            if (TerminalType == eTerminalType.Dumb || TerminalType == eTerminalType.Vt52)
            {
                return false;
            }
            else
            {
                var nawsRequest = new byte[] { 255, 253, 31 };

                var stream = TcpClient.GetStream();
                stream.Write(nawsRequest, 0, nawsRequest.Length);
                ReadInput();
                return true;
            }
        }

        public void SendOutput(string output, bool newLine = true)
        {
            /*
            if (newLine)
            {
                if (output.Last() != '\r' && output.Last() != '\r')
                {
                    output = output + Environment.NewLine;
                }
            }
            */
            var toSend = Encoding.Default.GetBytes(output);

            WriteStream(toSend);
        }

        private void WriteStream(byte[] buffer)
        {
            var stream = TcpClient.GetStream();

            stream.Write(buffer,0, buffer.Length);
            stream.Flush();
        }

        private void ReadInput()
        {
            if(TcpClient.Available == 0)
            {
                return;
            }

            ReadStream();
        }

        private void HandleByte(byte b)
        {
            var c = (char)b;

            if (!_clientEcho)
            {
                if (TcpClient.Connected)
                {
                    TcpClient.GetStream().Write(new byte[] { b }, 0, 1);
                }
            }

            if (c == '\r')
            {
                _lastWasCR = true;
                OnReturnPressed();
            }
            else if (c == '\n')
            {
                if (_lastWasCR)
                {
                    _lastWasCR = false;
                    return;
                }

                OnReturnPressed();
            }
            else if (char.IsControl(c))
            {
                if(c == '\b')
                {
                    _buffer = _buffer.Substring(0, int.Max(0, _buffer.Length - 1));
                }
            }
            else
            {
                _buffer = _buffer + c;
            }
        }

        private void OnReturnPressed()
        {
            ProcessCommand(_buffer);
            _buffer = string.Empty;
        }

        private void ProcessCommand(string command)
        {
            CurrentMenu.ProcessCommand(command,this);
            _buffer = string.Empty;
        }

        public void SetMenu(string menuName, bool addToStack)
        {

            if(menuName.Length > 0)
            {
                CurrentMenu = Menus[menuName];
            }

            SetMenu(CurrentMenu, addToStack);
        }

        public void SetMenu(Menu menu, bool addToStack)
        {
            if(addToStack)
            {
                _menuStack.Push(CurrentMenu);
            }
            CurrentMenu = menu;
            SendClear();
            CurrentMenu.Initialise();
            SendOutput(CurrentMenu.GetContent(Width,Height));
        }

        public void PopMenu()
        {
            if(_menuStack.Count > 0)
            {
                SetMenu(_menuStack.Pop(), false);
            }
        }

        private void ReadStream()
        {
            var stream = TcpClient.GetStream();
            var buffer = new byte[1024];

            while (TcpClient.Connected && stream.DataAvailable)
            {
                var read = stream.Read(buffer, 0, buffer.Length);

                if (read <= 0)
                {
                    break;
                }

                ParseTelnet(buffer, read);
            }
        }
        private void ParseTelnet(byte[] buffer,int bytesRead)
        {
            var i = 0;

            while (i < bytesRead)
            {
                var b = buffer[i];
                if(b == Telnet.IAC)
                {
                    i = HandleIAC(buffer, i, bytesRead);
                    continue;
                }
                HandleByte(b);
                ++i;

            }
        }

        private int HandleIAC(byte[] buffer, int idx,  int bytesRead)
        {
            if(idx + 1 >= bytesRead)
            {
                return bytesRead;
            }

            var cmd = buffer[idx + 1];

            switch(cmd)
            {
                case Telnet.WILL:
                case Telnet.WONT:
                case Telnet.DO:
                case Telnet.DONT:
                    return HandleNegotiation(buffer, idx, bytesRead);
                case Telnet.SB:
                    return HandleSubnegotiation(buffer,idx, bytesRead);
                case Telnet.SE:
                default:
                    return idx + 2;
            }
        }

        private int HandleNegotiation(byte[] buffer, int idx, int bytesRead)
        {
            if (idx + 2 > bytesRead)
            {
                return bytesRead;
            }

            var cmd = buffer[idx + 1];
            var option = buffer[idx + 2];

            switch (option)
            {
                case Telnet.OPT_ECHO:
                    HandleEchoNegotiation(cmd);
                    break;
                case Telnet.OPT_SUPPRESS_GO_AHEAD:
                    HandleSGA(cmd);
                    break;
            }


            return idx + 3;
        }

        private void HandleEchoNegotiation(byte cmd)
        {
            switch(cmd)
            {
                case Telnet.WILL:
                    _clientEcho = false;
                    break;
                case Telnet.WONT:
                    _clientEcho = true;
                    break;
                case Telnet.DO:
                    _serverEcho = true;
                    break;
                case Telnet.DONT:
                    _serverEcho = false;
                    break;
            }
        }

        private void HandleSGA(byte cmd)
        {
            _fullDuplex = (cmd == Telnet.WILL || cmd == Telnet.DO);
        }

        private int HandleSubnegotiation(byte[] buffer, int idx, int bytesRead)
        {
            if (idx + 2 > bytesRead)
            {
                return bytesRead;
            }

            var option = buffer[idx + 2];

            int i = idx + 3;

            while(i < bytesRead)
            {
                if (buffer[i] == Telnet.IAC && buffer[i+1] == Telnet.SE)
                {
                    ProcessSubnegotiation(option, buffer, idx + 3, i);
                    return i + 2;
                }
                ++i;
            }

            return bytesRead;
        }

        private void ProcessSubnegotiation(byte option, byte[] buffer, int start, int end)
        {
            switch(option)
            {
                case Telnet.OPT_NAWS:
                    ParseNAWS(buffer, start, end);
                    break;
                case Telnet.OPT_TERMINAL_TYPE:
                    break;
                default:
                    break;
            }
        }

        private void ParseNAWS(byte[] buffer, int start, int end)
        {
            if(end - start < 4)
            {
                return;
            }

            Width = (buffer[start] << 8) | buffer[start+1];
            Height = (buffer[start + 2] << 8) | buffer[start+3];
        }

        public void Disconnect()
        {
            if(TcpClient == null)
            {
                return;
            }

            TcpClient.Close();
            TcpClient.Dispose();
        }

        public void SendClear()
        {
            switch (TerminalType)
            {
                case eTerminalType.Modern:
                    SendOutput(Defines.ClearCommand, false);
                    break;
                case eTerminalType.Dumb:
                    WriteStream(new byte[] { 0x0C }); 
                    break;
                case eTerminalType.Vt52:
                    WriteStream(Encoding.ASCII.GetBytes("\x1BH")); 
                    WriteStream(Encoding.ASCII.GetBytes("\x1BJ")); 
                    break;
            }
        }
    }
}
