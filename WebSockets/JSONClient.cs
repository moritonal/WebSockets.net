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
        private WebSocketClient client;
        public Action<JObject> onMessageRecieved = null;

        public JSONClient(WebSocketClient client)
        {
            this.client = client;

            this.client.onMessageRecieved = (WebSocketMessage msg) =>
                {
                    this.onMessageRecieved(msg.DataAsJson);
                };
        }

        public void Send(JObject root)
        {
            this.client.SendPacket(root.ToString());
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