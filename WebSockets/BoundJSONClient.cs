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

        public Action<String, JObject> onDeadEnd;

        public BoundJSONClient(WebSocketClient client, bool bind = true) : base(client)
        {
            if (bind)
                this.webSocketClient.onMessageRecieved = (WebSocketMessage msg) =>
                    {
                        this.passMessage(msg);
                    };
        }

        public BoundJSONClient()
            : base(null)
        {

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
                return webSocketClient.onSocketClosed;
            }
            set
            {
                webSocketClient.onSocketClosed = value;
            }
        }

        public void passMessage(WebSocketMessage msg)
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
                else
                {
                    if (this.onDeadEnd.IsNotNull())
                        this.onDeadEnd(eventName, args);
                    
                }
            }
            else
            {
                throw new Exception("Could not understand message");
            }
        }
    }
}
