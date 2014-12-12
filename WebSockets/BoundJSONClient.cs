using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebSockets
{
    public class BoundJSONClient : JSONClient
    {
        public Dictionary<string, Action<JObject>> events = new Dictionary<string, Action<JObject>>();

        public BoundJSONClient(WebSocketClient client) : base(client)
        {
            this.client.onMessageRecieved = (WebSocketMessage msg) =>
                {
                    var obj = msg.DataAsJson;
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

        public Action onSocketClosed
        {
            get
            {
                return client.onSocketClosed;
            }
            set
            {
                client.onSocketClosed = value;
            }
        }
    }
}
