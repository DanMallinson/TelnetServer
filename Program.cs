using System.Data;
using System.Net;
using System.Net.Sockets;
using TelnetServer;

public static class Program
{
    static SortedDictionary<string, Menu> _menus;
    static void Main()

    {
        var alive = true;
        var listener = new TcpListener(IPAddress.Any, 23);
        var hostname = Dns.GetHostName();
        var entry = Dns.GetHostEntry(hostname);
        Console.WriteLine($"Listener started on:");
        foreach (var ipAddress in entry.AddressList)
        {
            Console.WriteLine(ipAddress.ToString());
        }

        LoadMenus();

        listener.Start();

        while (alive)
        {
            var client = listener.AcceptTcpClient();

            var clientThread = new ParameterizedThreadStart(OnClientConnected);
            var thread = new Thread(clientThread);
            thread.Start(client);
        }

        listener.Stop();
    }
    static void OnClientConnected(object? obj)
    {
        if (obj is not TcpClient client)
        {
            return;
        }

        var telnetClient = new TelnetClient()
        {
            TcpClient = client,
            Menus = _menus,
        };

        telnetClient.OnInitialise();

        while (telnetClient != null && !telnetClient.IsDisconnected())
        {
            telnetClient.Update();
        }

        if(telnetClient != null)
        {
            telnetClient.TcpClient.Close();
            telnetClient.TcpClient.Dispose();
        }
    }

    static void LoadMenus()
    {
        var defaultMenu = new NewsMenu();

        defaultMenu.Sources.Add(new Tuple<string, string>("BBC Top Stories", "https://feeds.bbci.co.uk/news/rss.xml"));
        defaultMenu.Sources.Add(new Tuple<string, string>("Eurogamer", "https://www.eurogamer.net/feed"));
        defaultMenu.Sources.Add(new Tuple<string, string>("Retro News", "https://www.retronews.com/feed/"));
        defaultMenu.Sources.Add(new Tuple<string, string>("Wargamer", "https://www.wargamer.com/mainrss.xml"));
        _menus = new SortedDictionary<string, Menu>();

        _menus["default"] = defaultMenu;
    }
}

