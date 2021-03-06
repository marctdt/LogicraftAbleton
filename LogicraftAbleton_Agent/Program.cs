﻿using System;
using System.Threading;
using System.Windows.Forms;

namespace LogicraftAbleton
{
	public static class Program
	{
		private static Mutex _mutex;

		[STAThread]
		public static void Main()
		{
			_mutex = new Mutex(true, "LogicraftAbleton", out var createdNew);

			if (!createdNew)
				//app is already running! Exiting the application  
				return;

			Application.EnableVisualStyles();
			Application.Run(new LogicraftForm());
		}
	}
}