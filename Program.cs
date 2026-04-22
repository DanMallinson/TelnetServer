using System.Data;
using System.Net;
using System.Net.Sockets;
using System.Xml.Linq;
using TelnetServer;

public static class Program
{
    static SortedDictionary<string, Menu> _menus;
    static void Main()

    {
        var alive = true;
        var listener = new TcpListener(IPAddress.Any, 8080);
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

        const string opmlPath = "feeds.opml";
        if (File.Exists(opmlPath))
        {
            try
            {
                var doc = XDocument.Load(opmlPath);
                var outlines = doc.Descendants("outline")
                    .Where(o => o.Attribute("xmlUrl") != null);

                foreach (var outline in outlines)
                {
                    var name = outline.Attribute("text")?.Value ?? "Unknown";
                    var url  = outline.Attribute("xmlUrl")!.Value;
                    defaultMenu.Sources.Add(new Tuple<string, string>(name, url));
                }

                Console.WriteLine($"Loaded {defaultMenu.Sources.Count} feed(s) from {opmlPath}.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load {opmlPath}: {ex.Message}. Using default feeds.");
                LoadDefaultSources(defaultMenu);
            }
        }
        else
        {
            Console.WriteLine($"{opmlPath} not found. Using default feeds.");
            LoadDefaultSources(defaultMenu);
        }

        _menus = new SortedDictionary<string, Menu>();
        _menus["default"] = defaultMenu;
    }

    static void LoadDefaultSources(NewsMenu menu)
    {
        menu.Sources.Add(new Tuple<string, string>("BBC Top Stories", "https://feeds.bbci.co.uk/news/rss.xml"));
        menu.Sources.Add(new Tuple<string, string>("Eurogamer", "https://www.eurogamer.net/feed"));
        menu.Sources.Add(new Tuple<string, string>("Retro News", "https://www.retronews.com/feed/"));
        menu.Sources.Add(new Tuple<string, string>("Wargamer", "https://www.wargamer.com/mainrss.xml"));
        menu.Sources.Add(new Tuple<string, string>("Amstrad", "https://www.reddit.com/r/Amstrad.rss"));
    }
}

