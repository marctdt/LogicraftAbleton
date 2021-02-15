using System;
using System.Linq;
using WebSocketSharp.Server;

namespace LogicraftAbleton.Helpers
{
	public class ClosingWsBehavior : WebSocketBehavior
	{
		public event EventHandler CloseRequested;

		protected override void OnOpen()
		{
			CloseRequested?.Invoke(this, null);
			Sessions.Sessions.ToList().ForEach(x => Sessions.CloseSession(x.ID));
			base.OnOpen();
		}
	}
}