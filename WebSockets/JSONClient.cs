using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets
{
    public class JSONClient
    {
        protected WebSocketClient webSocketClient;

        public JSONClient(WebSocketClient client)
        {
            this.webSocketClient = client;
        }

        public static implicit operator WebSocketClient(JSONClient client)
        {
            return client.webSocketClient;
        }

        public void Send(JObject root)
        {
            this.webSocketClient.SendPacket(root.ToString());
        }

        public void Send(string msg, params KeyValuePair<string, string>[] args)
        {
            JObject a = new JObject();

            foreach (var arg in args)
            {
                a.Add(arg.Key, arg.Value);
            }

            this.Send(msg, a);
        }

        public void Send(string msg, JObject args)
        {
            JObject root = new JObject();

            root["event"] = msg;
            root["data"] = args;

            this.Send(root);
        }

        public void Send(string msg)
        {
            JObject root = new JObject();

            root["event"] = msg;
            root["data"] = new JObject();

            this.Send(root);
        }
    }
}