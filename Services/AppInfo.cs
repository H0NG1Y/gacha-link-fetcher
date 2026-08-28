using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace GachaLinkFetcher.Services
{
    internal static class AppInfo
    {
        public const string RepositoryUrl = "https://github.com/H0NG1Y/gacha-link-fetcher";
        public const string LatestReleaseApiUrl = "https://api.github.com/repos/H0NG1Y/gacha-link-fetcher/releases/latest";
        public const string NodeCatalogUrl = "https://github.akams.cn/";

        public static Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0); }
        }

        public static string VersionText
        {
            get
            {
                var attribute = Attribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyInformationalVersionAttribute)) as AssemblyInformationalVersionAttribute;
                return attribute == null || string.IsNullOrWhiteSpace(attribute.InformationalVersion) ? Version.ToString(3) : attribute.InformationalVersion;
            }
        }

        public static string ExecutablePath
        {
            get
            {
                try { return Process.GetCurrentProcess().MainModule.FileName; }
                catch (Exception) { return Assembly.GetExecutingAssembly().Location; }
            }
        }

        public static string InstallDirectory
        {
            get { return Path.GetDirectoryName(ExecutablePath) ?? string.Empty; }
        }
    }
}
