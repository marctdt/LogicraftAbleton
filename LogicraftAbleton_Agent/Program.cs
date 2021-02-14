using System;
using System.Threading;
using System.Windows.Forms;

namespace LogicraftAbleton
{
	public static class Program
	{
		private static Mutex _mutex = null;
		[STAThread]
		public static void Main()
		{
			bool createdNew;
			_mutex = new Mutex(true, "LogicraftAbleton", out createdNew);

			if (!createdNew)
			{
				//app is already running! Exiting the application  
				return;
			}

			Application.EnableVisualStyles();
			Application.Run(new LogiCraftForm());
		}
	}
}
