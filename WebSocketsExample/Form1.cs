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
                this.txtBox1.Invoke(new Action(() =>
                {
                    this.txtBox1.AppendText((str.Length > 250 ? str.Substring(0, 250) : str) + Environment.NewLine);
                }));
            };

            server.onClientJoined = (WebSocketClient client) =>
            {
                this.server.Log("WebSocket client joined");

                var json = new BoundJSONClient(client);

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

                        foreach (var i in this.server.JsonClients)
                            i.Send("chat", data);
                    };
                json["keyDown"] = x =>
                    {
                        json.Send("keyDown",
                            new KeyValuePair<string, string>("msg", parameters["name"] + ": " + (string)x["message"]));
                    };

            };

            server.onHttpRequest = (HttpSocketClient client) =>
                {
                    var r = client.handshake.request;

                    r =  r == "/" ? "\\index.html" : r;
                    r = r.Replace('/', '\\');

                    this.server.Log("Served " + r);

                    client.Write(HttpServer.ServeHttpPage(Environment.CurrentDirectory + "\\..\\..\\..\\WebSocketsExample Web-Page" + r));
                };

            server.Init();
        }

        private void btnCommand_Click(object sender, EventArgs e)
        {
            foreach (var i in this.server.JsonClients)
                i.Send(txtCommand.Text, new KeyValuePair<string, string>("data", txtData.Text));
        }
    }
}