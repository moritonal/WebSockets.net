using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebSockets
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
                client.onMessageRecieved = (WebSocketMessage msg) =>
                {
                    client.SendPacket(String.Join("", WebSocketClient.Encoder.GetString(msg.data.ToArray())));
                };
            };

            server.Init();

            Timer timer = new Timer();
            timer.Tick += timer_Tick;
            timer.Interval = 1000;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            this.server.Clients.Values.ToList().ForEach(x => x.SendPacket(DateTime.Now.ToLongTimeString()));
        }
    }
}
