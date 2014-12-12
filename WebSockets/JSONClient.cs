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
        protected WebSocketClient client;
        public Action<JObject> onMessageRecieved = null;

        public JSONClient(WebSocketClient client)
        {
            this.client = client;
        }

        public void Send(JObject root)
        {
            this.client.SendPacket(root.ToString());
        }

        public void Send(string p, params KeyValuePair<string, string>[] args)
        {
            JObject a = new JObject();

            foreach (var arg in args)
            {
                a.Add(arg.Key, arg.Value);
            }

            this.Send(p, a);
        }

        public void Send(string p, JObject a)
        {
            JObject root = new JObject();

            root["event"] = p;
            root["data"] = a;

            this.Send(root);
        }

        public void Send(string p)
        {
            JObject root = new JObject();

            root["event"] = p;
            root["data"] = new JObject();

            this.Send(root);
        }
    }
}