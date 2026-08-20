using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using GachaLinkFetcher.Models;
using GachaLinkFetcher.Services;

namespace GachaLinkFetcher.Storage
{
    internal sealed class LocalStore
    {
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = 67108864 };
        public string DataDirectory { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GachaLinkFetcher"); } }
        public string DataPath { get { return Path.Combine(DataDirectory, "records.json"); } }
        public string BackupDirectory { get { return Path.Combine(DataDirectory, "backups"); } }

        public LocalDatabase Load()
        {
            try { if (File.Exists(DataPath)) return serializer.Deserialize<LocalDatabase>(File.ReadAllText(DataPath)) ?? new LocalDatabase(); }
            catch (Exception) { }
            return new LocalDatabase();
        }
        public int Merge(IEnumerable<GachaRecord> records)
        {
            var database = Load(); var known = new HashSet<string>(database.Records.Select(CanonicalKey), StringComparer.Ordinal); var added = 0;
            foreach (var record in records) if (known.Add(CanonicalKey(record))) { database.Records.Add(record); added++; }
            if (added > 0) Save(database, true);
            return added;
        }
        private static string CanonicalKey(GachaRecord record)
        {
            GameKind game;
            var pool = Enum.TryParse(record.Game, out game) ? GachaPoolCatalog.CanonicalCode(game, record.GachaType) : record.GachaType;
            return record.Game + "|" + record.Uid + "|" + pool + "|" + (string.IsNullOrWhiteSpace(record.Id) ? record.Time + "|" + record.Name : record.Id);
        }
        public void Save(LocalDatabase database, bool backup)
        {
            Directory.CreateDirectory(DataDirectory); Directory.CreateDirectory(BackupDirectory);
            if (backup && File.Exists(DataPath)) File.Copy(DataPath, Path.Combine(BackupDirectory, "records-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json"), true);
            File.WriteAllText(DataPath, serializer.Serialize(database));
            TrimBackups();
        }
        public string CreateBackup()
        {
            Directory.CreateDirectory(BackupDirectory); var target = Path.Combine(BackupDirectory, "records-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
            Save(Load(), false); File.Copy(DataPath, target, true); TrimBackups(); return target;
        }
        public void Restore(string source)
        {
            var database = serializer.Deserialize<LocalDatabase>(File.ReadAllText(source)); if (database == null || database.Records == null) throw new InvalidDataException("备份文件格式无效。");
            Save(database, true);
        }
        private void TrimBackups()
        {
            try { foreach (var file in new DirectoryInfo(BackupDirectory).GetFiles("records-*.json").OrderByDescending(item => item.LastWriteTimeUtc).Skip(20)) file.Delete(); } catch (IOException) { }
        }
    }
}
