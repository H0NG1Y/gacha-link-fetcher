using System;
using System.IO;
using System.Web.Script.Serialization;
using GachaLinkFetcher.Models;

namespace GachaLinkFetcher.Storage
{
    internal sealed class SettingsStore
    {
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer();

        public string DataDirectory
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GachaLinkFetcher"); }
        }

        public string SettingsPath
        {
            get { return Path.Combine(DataDirectory, "settings.json"); }
        }

        public AppSettings Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return new AppSettings();
                var settings = serializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath));
                return settings ?? new AppSettings();
            }
            catch (Exception)
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            if (settings == null) throw new ArgumentNullException("settings");
            Directory.CreateDirectory(DataDirectory);
            var temporaryPath = SettingsPath + ".tmp";
            File.WriteAllText(temporaryPath, serializer.Serialize(settings));
            if (File.Exists(SettingsPath)) File.Delete(SettingsPath);
            File.Move(temporaryPath, SettingsPath);
        }
    }
}
