using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WebSockets;

namespace WebSocketsExample
{
    public partial class Form1 : Form
    {
        WebSocketServer server = new WebSocketServer(9090);

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            server.onLog = (string str) =>
            {
                this.textBox1.Invoke(new Action(() =>
                {
                    this.textBox1.AppendText(str + Environment.NewLine);
                }));
            };

            server.onClientJoined = (WebSocketClient client) =>
            {
                var json = new BoundJSONClient(new JSONClient(client));

                json["chat"] = (JObject obj) =>
                    {
                        JObject data = new JObject();

                        data["msg"] = (string)obj["message"];

                        json.Send("chat", data);
                    };
            };

            server.Init();
        }

    }
}
