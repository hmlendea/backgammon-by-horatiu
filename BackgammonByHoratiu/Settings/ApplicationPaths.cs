using System;
using System.IO;
using System.Reflection;

namespace BackgammonByHoratiu.Settings
{
    public static class ApplicationPaths
    {
        static readonly string rootDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        public static string UserDataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BackgammonByHoratiu");

        public static string SettingsFile => Path.Combine(UserDataDirectory, "Settings.xml");
    }
}
