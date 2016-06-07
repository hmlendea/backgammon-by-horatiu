using System.IO;
using System.Reflection;

using Gtk;

using BackgammonByHoratiu.Utils;

namespace BackgammonByHoratiu
{
    class MainClass
    {
        public static void Main(string[] args)
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();
            string binPath;

            binPath = Path.GetDirectoryName(thisAssembly.Location);

            Logger.MainLog.WriteLine(
                "Starting " + thisAssembly.GetName().Name +
                " v" + thisAssembly.GetName().Version);

            Directory.SetCurrentDirectory(binPath);

            Application.Init();
            MainWindow win = new MainWindow();
            win.Show();

            Application.Run();
        }
    }
}
