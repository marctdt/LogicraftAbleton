using System;
using System.Windows.Forms;

namespace LogicraftAbleton
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            Application.EnableVisualStyles();

            Application.Run(new LogiCraftForm());
        }
    }
}
