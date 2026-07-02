using System;
using System.IO;

namespace BackgammonByHoratiu.Settings
{
    public static class ApplicationPaths
    {
        public static string UserDataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BackgammonByHoratiu");

        public static string SettingsFile => Path.Combine(UserDataDirectory, "Settings.xml");
    }
}
