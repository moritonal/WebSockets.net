using Gma.QrCodeNet.Encoding;
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
                json["setRot"] = x =>
                    {
                        foreach (var i in this.server.JsonClients)
                            i.Send("setRot",
                                new KeyValuePair<string, string>("x", (string)x["x"]),
                                new KeyValuePair<string, string>("y", (string)x["y"]),
                                new KeyValuePair<string, string>("z", (string)x["z"]));
                    };
            };

            server.onHttpRequest = (HttpSocketClient client) =>
                {
                    var r = client.handshake.request;

                    r =  r == "/" ? "\\index.html" : r;
                    r = r.Replace('/', '\\');

                    this.server.Log("Served " + r);

                    var type = "text/html";
                    if (r.EndsWith(".css"))
                        type = "text/css";
                    else if (r.EndsWith(".js"))
                        type = "text/javascript";

                    client.Write(HttpServer.ServeHttpPage(Environment.CurrentDirectory + "\\..\\..\\..\\WebSocketsExample Web-Page" + r, type));
                };

            server.Init();

            var encoder = new QrEncoder(ErrorCorrectionLevel.H);
            var code = encoder.Encode("https://" + server.Address);
            var bitmap = new Bitmap(code.Matrix.Width * 5, code.Matrix.Height * 5);

            for (int x = 0; x < code.Matrix.Width; x++)
                for (int y = 0; y < code.Matrix.Height; y++)
                    for (int xx = 0; xx < 5; xx++)
                        for (int yy = 0; yy < 5; yy++)
                            bitmap.SetPixel(x*5 + xx, y*5 + yy, code.Matrix[x, y] ? Color.Black : Color.White);

            this.pictureBox1.Image = bitmap;
        }

        private void btnCommand_Click(object sender, EventArgs e)
        {
            foreach (var i in this.server.JsonClients)
                i.Send(txtCommand.Text, new KeyValuePair<string, string>("data", txtData.Text));
        }
    }
}