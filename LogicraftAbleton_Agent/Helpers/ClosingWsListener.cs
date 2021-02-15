using System;
using WebSocketSharp.Server;

namespace LogicraftAbleton.Helpers
{
	public class ClosingWsListener
	{
		private readonly WebSocketServer _webSocketServerListener = new WebSocketServer(9009);

		public ClosingWsListener()
		{
			_webSocketServerListener.AddWebSocketService<ClosingWsService>("/close", x =>
			 {
				 OnCloseRequest?.Invoke(this, null);
			 });
			_webSocketServerListener.Start();
		}

		public event EventHandler OnCloseRequest;
	}

	public class ClosingWsService:WebSocketBehavior
	{
	}
}