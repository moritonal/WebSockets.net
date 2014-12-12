using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets
{
    public class WebSocketHandshake
    {
        public string method;
        public string request;
        public string protocol;

        public Dictionary<string, string> headers;

        public WebSocketHandshake(string str)
        {
            if (str != "")
            {
                var _headers = str.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList().Select(x => x.Split(':'));

                var _getHeader = _headers.Where(x => x[0].StartsWith("GET"));

                if (_getHeader.FirstOrDefault().IsNotNull())
                {
                    var getHeader = _getHeader.FirstOrDefault().First().Split(' ');

                    method = getHeader[0];
                    request = getHeader[1];
                    protocol = getHeader[2];

                    headers = new Dictionary<string, string>();
                    foreach (var header in _headers.Where(x => x.Count() > 1))
                    {
                        headers[header[0].Trim().ToLower()] = String.Join(":", header.Skip(1).ToArray()).Trim();
                    }
                }
            }
        }

        public string this[string key]
        {
            get
            {
                if (this.headers.ContainsKey(key))
                    return this.headers[key];
                else
                    return null;
            }
        }

        public bool Valid
        {
            get
            {
                return headers.IsNotNull();
            }
        }

        public string Protocol
        {
            get
            {
                var connectionMode = this["connection"];
                var upgradeMode = this["upgrade"];
                if (connectionMode.IsNotNull())
                {
                    if (upgradeMode.IsNotNull() && upgradeMode.IsNotNull() && upgradeMode.ToLower() == "websocket")
                    {
                        return "ws";
                    }
                    else if (connectionMode.ToLower().Split(',').Contains("keep-alive"))
                    {
                        return "http";
                    }
                }
                return null;
            }
        }
    }
}
