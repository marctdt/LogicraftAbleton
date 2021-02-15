using System;
using WebSocketSharp.Server;

namespace LogicraftAbleton.Helpers
{
	public class ClosingWsListener
	{
		private readonly WebSocketServer _webSocketServerListener = new WebSocketServer(9009);

		public ClosingWsListener()
		{
			void ClosingBehavior(ClosingWsBehavior wsService)
			{
				wsService.CloseRequested += (sender, args) => OnCloseRequest?.Invoke(this, null);
			}

			_webSocketServerListener.AddWebSocketService<ClosingWsBehavior>("/close", ClosingBehavior);
			_webSocketServerListener.Start();
		}

		public event EventHandler OnCloseRequest;
	}
}