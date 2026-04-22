using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace TelnetServer
{
    internal class HeadlineMenu : Menu
    {
        public string Title { get; set; } = string.Empty;
        public string Feed {  get; set; } = string.Empty;

        public List<Tuple<string, DateTime, string,string>> Headlines { get; set; } = new List<Tuple<string, DateTime, string,string>>();

        private int _page = 0;

        public override string GetContent(int width, int height)
        {
            var builder = new StringBuilder();
            builder.Append(Defines.GetHeading(Title,width)+"\r\n");
            var footer = string.Empty;

            var pageSize = height - 5;
            var remaining = Headlines.Count - (pageSize * _page);

            var count = Headlines.Count;

            if(count > pageSize)
            {
                count = height - 6;
                pageSize = height - 6;
            }

            for(var i =0; i < count; ++i)
            {
                var idx = pageSize * _page + i;

                if (idx >= Headlines.Count)
                {
                    builder.Append("\r\n");
                }
                else
                {
                    var headline = Headlines[idx].Item1;
                    var num = i.ToString();
                    if(headline.Length + num.Length >= width - 3)
                    {
                        headline = headline.Substring(0,(width - num.Length)-3);
                    }
                    builder.Append($"{num}\t{headline}\r\n");
                }
            }

            if(_page > 0)
            {
                footer += " - Prev Page ";
            }

            if(remaining > pageSize)
            {
                footer += " + Next Page ";
            }

            builder.Append(Defines.GetHeading(footer, width)+"\r\n");


            return builder.ToString();
        }

        protected override void OnInitialise()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", "CSharpApp/1.0");
            var result = httpClient.GetStringAsync(Feed).Result;

            var xml = new XmlDocument();
            xml.LoadXml(result);

            var items = xml.GetElementsByTagName("item");
            if(items.Count == 0)
            {
                items = xml.GetElementsByTagName("entry");
            }
            Headlines.Clear();

            foreach (XmlElement item in items)
            {
                var title = item.GetElementsByTagName("title")[0].InnerText;
                var pub = item.GetElementsByTagName("pubDate");
                var timestamp = DateTime.MinValue;
                if(pub == null || pub.Count == 0)
                {
                    pub = item.GetElementsByTagName("published");
                }
                if(pub != null && pub.Count > 0)
                {
                    DateTime.TryParse(pub[0].InnerText,out timestamp);
                }

                var url = item.GetElementsByTagName("link")[0].InnerText;
                var cont = item.GetElementsByTagName("content");
                var content = string.Empty;
                if(cont == null || cont.Count == 0)
                {
                    cont = item.GetElementsByTagName("content:encoded");
                }
                if(cont != null && cont.Count > 0)
                {
                    content = cont[0].InnerText;
                }
                Headlines.Add(new Tuple<string, DateTime, string,string>(title, timestamp, url,content));
            }

            Headlines = Headlines.OrderByDescending(x=>x.Item2).ThenBy(x=>x.Item1).ToList();

        }

        protected override void ProcessCommandInternal(string command, TelnetClient client)
        {
            if (int.TryParse(command, out int option))
            {
                var pageSize = client.Height - 6;

                var idx = pageSize * _page + option;

                if (idx < Headlines.Count)
                {
                    var story = new StoryMenu()
                    {
                        Url = Headlines[idx].Item3,
                        Timestamp = Headlines[idx].Item2,
                        Title = Headlines[idx].Item1,
                        Article = Headlines[idx].Item4
                    };

                    client.SetMenu(story,true);
                }
            }
            else
            {
                if (command == "+")
                {
                    //Check we don't go too far
                    ++_page;
                }
                else if (command == "-")
                {
                    if (_page > 0)
                    {
                        --_page;
                    }
                }

                client.SendClear();
                client.SendOutput(GetContent(client.Width, client.Height));
            }
        }

    }
}
