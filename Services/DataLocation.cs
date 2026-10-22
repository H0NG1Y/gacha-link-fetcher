using System;
using System.IO;

namespace GachaLinkFetcher.Services
{
    /// <summary>
    /// 统一决定记录、设置、备份和下载缓存的存放位置。
    /// 优先使用程序所在目录下的 data 子目录（免安装版放在任何可写目录都能直接用，也不需要管理员权限）；
    /// 该目录不可写时（例如安装到 Program Files）自动回退到 %LocalAppData%\GachaLinkFetcher。
    /// 首次切换到新位置时会从旧位置迁移已有记录、设置和备份。
    /// </summary>
    internal static class DataLocation
    {
        private const string FolderName = "GachaLinkFetcher";
        private static readonly object Gate = new object();
        private static string resolvedRoot;

        public static string PortableRoot
        {
            get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data"); }
        }

        public static string UserRoot
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), FolderName); }
        }

        public static string Root
        {
            get
            {
                lock (Gate)
                {
                    if (resolvedRoot == null) resolvedRoot = Resolve();
                    return resolvedRoot;
                }
            }
        }

        public static bool IsPortable
        {
            get { return string.Equals(Root, PortableRoot, StringComparison.OrdinalIgnoreCase); }
        }

        private static string Resolve()
        {
            var candidates = new[] { PortableRoot, UserRoot };
            string chosen = null;
            for (var index = 0; index < candidates.Length && chosen == null; index++)
                if (IsWritable(candidates[index])) chosen = candidates[index];
            if (chosen == null) chosen = UserRoot;
            Migrate(chosen, candidates);
            return chosen;
        }

        private static bool IsWritable(string directory)
        {
            try
            {
                Directory.CreateDirectory(directory);
                var probe = Path.Combine(directory, "write-probe-" + Guid.NewGuid().ToString("N") + ".tmp");
                File.WriteAllText(probe, string.Empty);
                File.Delete(probe);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void Migrate(string target, string[] candidates)
        {
            try
            {
                var targetRecords = Path.Combine(target, "records.json");
                if (File.Exists(targetRecords)) return;
                foreach (var candidate in candidates)
                {
                    if (string.Equals(candidate, target, StringComparison.OrdinalIgnoreCase)) continue;
                    var sourceRecords = Path.Combine(candidate, "records.json");
                    if (!File.Exists(sourceRecords)) continue;
                    CopyIfMissing(sourceRecords, targetRecords);
                    CopyIfMissing(Path.Combine(candidate, "settings.json"), Path.Combine(target, "settings.json"));
                    var sourceBackups = Path.Combine(candidate, "backups");
                    if (Directory.Exists(sourceBackups))
                    {
                        var targetBackups = Path.Combine(target, "backups");
                        Directory.CreateDirectory(targetBackups);
                        foreach (var file in Directory.GetFiles(sourceBackups, "records-*.json"))
                            CopyIfMissing(file, Path.Combine(targetBackups, Path.GetFileName(file)));
                    }
                    return;
                }
            }
            catch (Exception)
            {
            }
        }

        private static void CopyIfMissing(string source, string target)
        {
            if (File.Exists(target)) return;
            var parent = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
            File.Copy(source, target, false);
        }
    }
}
