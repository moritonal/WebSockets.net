using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets
{
    public class BoundJSONClient
    {
        private JSONClient jSONClient;
        public Dictionary<string, Action<JObject>> events = new Dictionary<string, Action<JObject>>();

        public BoundJSONClient(JSONClient jSONClient)
        {
            this.jSONClient = jSONClient;

            this.jSONClient.onMessageRecieved = (JObject obj) =>
                {
                    if (obj.IsNotNull())
                    {
                        var eventName = (string)obj["event"];
                        var args = (JObject)obj["data"];
                        if (this.events.ContainsKey(eventName))
                        {
                            this.events[eventName](args);
                        }
                    }
                };
        }

        public Action<JObject> this[string key]
        {
            get
            {
                return events[key];
            }
            set
            {
                events[key] = value;
            }
        }

        public void Send(string p, JObject a)
        {
            JObject root = new JObject();

            root["event"] = p;
            root["data"] = a;

            this.jSONClient.Send(root);
        }

        public void Send(string p)
        {
            JObject root = new JObject();

            root["event"] = p;
            root["data"] = new JObject();

            this.jSONClient.Send(root);
        }
    }
}
