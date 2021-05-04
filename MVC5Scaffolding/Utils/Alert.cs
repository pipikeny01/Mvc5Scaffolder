using System.Windows;

namespace Happy.Scaffolding.MVC.Utils
{
    public class Alert
    {
        public static void Trace(string msg)
        {
            MessageBox.Show(msg);
        }
    }
}