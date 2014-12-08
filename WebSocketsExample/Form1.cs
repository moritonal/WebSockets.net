using HttpHelper;
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
        WebSocketServer server = new WebSocketServer(8000);

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

                Dictionary<string, string> parameters = new Dictionary<string, string>();

                json["login"] = x =>
                    {
                        parameters["name"] = (string)x["name"];
                        json.Send("loginAccepted");
                    };

                json["chat"] = x =>
                    {
                        JObject data = new JObject();

                        data["msg"] = parameters["name"] + ": " + (string)x["message"];

                        json.Send("chat", data);
                    };
            };

            server.onHttpRequest = (HttpSocketClient client) =>
                {
                    var r = client.handshake.request;

                    r =  r == "/" ? "\\index.html" : r;
                    r = r.Replace('/', '\\');

                    client.Write(HttpServer.ServeHttpPage(Environment.CurrentDirectory + "\\..\\..\\..\\WebSocketsExample Web-Page" + r));
                };

            server.Init();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            server.Clients
                .Values
                .Select(x => x as WebSocketClient)
                .Where(x => x != null)
                .Select(x => new BoundJSONClient(new JSONClient(x)))
                .ToList()
                .ForEach(x =>
            {
                JObject obj = new JObject();
                obj["msg"] = textBox2.Text;
                x.Send("notification", obj);
            });
        }
    }
}