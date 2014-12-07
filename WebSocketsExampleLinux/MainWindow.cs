using System;
using Gtk;
using WebSockets;

public partial class MainWindow: Gtk.Window
{	
	WebSocketServer server = new WebSocketServer(9090);

	public MainWindow (): base (Gtk.WindowType.Toplevel)
	{
		Build ();

		server.onClientJoined = (WebSocketClient client) =>
		{
			client.onMessageRecieved = (WebSocketMessage msg) =>
			{
				client.SendPacket(msg.DataAsString);
			};
		};

		server.Init ();
	}

	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}
}
