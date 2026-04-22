using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace TelnetServer
{
    internal class StoryMenu : Menu
    {
        public string Title { get; set; }
        public DateTime Timestamp { get; set; }
        public string Url { get; set; } = string.Empty;
        public string Article {get; set;} = string.Empty;

        private int _page = 0;
        private List<string> _content = new List<string>();

        protected override void OnInitialise()
        {
            var result = string.Empty;

            if(!string.IsNullOrEmpty(Article))
            {
                result=  Article;
            }
            else
            {
                var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Add("User-Agent", "CSharpApp/1.0");
                result = httpClient.GetStringAsync(Url).Result;
            }
            var document = new HtmlDocument();
            document.LoadHtml(result);
            
            var node = document.DocumentNode;
            if(string.IsNullOrEmpty(Article))
            {
                node = document.DocumentNode.SelectSingleNode("//main");
                if(node == null)
                {
                    node = document.DocumentNode.SelectSingleNode("//body");
                }
            }
            
            _content = new List<string>(GetTextInNode(node).Split(Environment.NewLine));
        }

        private string GetTextInNode(HtmlNode node)
        {
            var builder = new StringBuilder();
            foreach(var child in node.ChildNodes)
            {
                if(child.NodeType == HtmlNodeType.Text)
                {
                    builder.Append(System.Net.WebUtility.HtmlDecode(child.InnerText.Trim())+"\r\n");
                }
                else
                {
                    var text = GetTextInNode(child).Trim();
                    if(!string.IsNullOrEmpty(text))
                    {
                        builder.Append(text);
                        builder.Append("\r\n");
                    }
                }
            }
            return builder.ToString();
        }

        public override string GetContent(int width, int height)
        {
            var builder = new StringBuilder();
            var footer = string.Empty;

            if (_page == 0)
            {
                builder.Append(GetHeader(width)+"\r\n");
            }
            else
            {
                builder.Append(Defines.GetHeading(Title,width)+"\r\n");
            }
            //We might want to cache this if it doesn't change between calls
            var resized = ResizeContent(_content, width);
            var pageSize = height - 5;

            for(var i = 0; i < pageSize; ++i)
            {
                var idx = pageSize * _page + i;

                if(idx < resized.Count)
                {
                    builder.Append(resized[idx]+"\r\n");
                }
                else
                {
                    builder.Append("\r\n");
                }

            }

            if (_page > 0)
            {
                footer += " - Prev Page ";
            }

            if (pageSize * (_page + 1) < resized.Count)
            {
                footer += " + Next Page ";
            }

            builder.Append(Defines.GetHeading(footer,width)+"\r\n");

            return builder.ToString();
        }

        private List<string> ResizeContent(List<string> content, int width)
        {
            var result = new List<string>();

            for(var i = 0; i < content.Count; ++i)
            {
                if (content[i].Length < width)
                {
                    result.Add(content[i]);
                }
                else
                {
                    var text = content[i];
                    while (text.Length >= width)
                    {
                        var spaceIndex = content[i].LastIndexOf(' ', Math.Min(width-1, content[i].Length - 1));
                        if(spaceIndex == -1)
                        {
                            spaceIndex = width - 1;
                        }

                        result.Add(text.Substring(0, spaceIndex-1));
                        text = text.Substring(spaceIndex );
                    }
                    result.Add(text);

                }
            }
            return result;
        }

        private string GetHeader(int width)
        {
            var builder = new StringBuilder();
            var combined = Title + " - " + Timestamp.ToString("dd MMM yyyy HH:mm");
            if (Title.Length >= width || combined.Length >= width)
            {
                builder.Append(Defines.GetHeading(Title,width)+"\r\n");
                builder.Append(Defines.GetHeading(Timestamp.ToString("dd MMM yyyy HH:mm"),width)+"\r\n");
            }
            else
            {
                builder.Append(Defines.GetHeading(combined, width)+"\r\n");
            }
            return builder.ToString();
        }
        protected override void ProcessCommandInternal(string command, TelnetClient client)
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
