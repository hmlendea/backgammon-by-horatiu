using System.IO;
using System.Threading;

using NuciDAL.IO;

namespace BackgammonByHoratiu.Settings
{
    public class SettingsManager
    {
        static volatile SettingsManager instance;
        static readonly Lock syncRoot = new();

        public static SettingsManager Instance
        {
            get
            {
                if (instance is null)
                {
                    lock (syncRoot)
                    {
                        instance ??= new SettingsManager();
                    }
                }

                return instance;
            }
        }

        public AudioSettings AudioSettings { get; set; }

        public GraphicsSettings GraphicsSettings { get; set; }

        public bool DebugMode { get; set; }

        public SettingsManager()
        {
            AudioSettings = new AudioSettings();
            GraphicsSettings = new GraphicsSettings();
        }

        public void LoadContent()
        {
            if (!File.Exists(ApplicationPaths.SettingsFile))
            {
                SaveContent();
                return;
            }

            XmlFileObject<SettingsManager> xmlManager = new();
            SettingsManager storedSettings = xmlManager.Read(ApplicationPaths.SettingsFile);

            instance = storedSettings;
        }

        public void SaveContent()
        {
            XmlFileObject<SettingsManager> xmlManager = new();
            xmlManager.Write(ApplicationPaths.SettingsFile, this);
        }

        public void Update() { }
    }
}
