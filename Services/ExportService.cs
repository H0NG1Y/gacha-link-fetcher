using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Web.Script.Serialization;
using GachaLinkFetcher.Models;

namespace GachaLinkFetcher.Services
{
    internal sealed class ExportService
    {
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = 67108864 };
        private static readonly string[] Headers = { "Game", "UID", "ID", "Banner Code", "API Banner Code", "Banner Name", "Name", "Type", "Rank", "Item ID", "Count", "Time" };
        public void ExportCsv(IEnumerable<GachaRecord> source, string path)
        {
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.WriteLine(string.Join(",", Headers)); foreach (var item in source) writer.WriteLine(string.Join(",", Values(item).Select(Csv)));
            }
        }
        public void ExportExcelXml(IEnumerable<GachaRecord> source, string path)
        {
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.Write("<?xml version=\"1.0\"?><Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\"><Worksheet ss:Name=\"Gacha Records\"><Table>");
                writer.Write(Row(Headers)); foreach (var item in source) writer.Write(Row(Values(item))); writer.Write("</Table></Worksheet></Workbook>");
            }
        }
        public void ExportJson(IEnumerable<GachaRecord> source, string path) { File.WriteAllText(path, serializer.Serialize(source.ToList()), new UTF8Encoding(true)); }
        public void ExportUigf(IEnumerable<GachaRecord> source, string path)
        {
            var records = source.ToList();
            if (records.Any(item => item.Game == GameKind.WutheringWaves.ToString())) throw new InvalidOperationException("鸣潮记录请使用通用 JSON 导出；UIGF 不适用于该游戏。");
            var list = records.Select(item => new Dictionary<string, string> { { "uid", item.Uid }, { "uigf_gacha_type", GachaPoolCatalog.CanonicalCode(item) }, { "gacha_type", string.IsNullOrWhiteSpace(item.ApiGachaType) ? item.GachaType : item.ApiGachaType }, { "item_id", item.ItemId }, { "count", item.Count }, { "time", item.Time }, { "name", item.Name }, { "lang", "zh-cn" }, { "item_type", item.ItemType }, { "rank_type", item.RankType }, { "id", item.Id } }).ToList();
            var root = new Dictionary<string, object> { { "info", new Dictionary<string, string> { { "uigf_version", "v4.0" }, { "export_timestamp", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() }, { "export_app", "GachaLinkFetcher" }, { "export_app_version", "3.0.0" } } }, { "list", list } };
            File.WriteAllText(path, serializer.Serialize(root), new UTF8Encoding(true));
        }
        private static string[] Values(GachaRecord item) { return new[] { item.Game, item.Uid, item.Id, GachaPoolCatalog.CanonicalCode(item), string.IsNullOrWhiteSpace(item.ApiGachaType) ? item.GachaType : item.ApiGachaType, GachaPoolCatalog.NameFor(item), item.Name, item.ItemType, item.RankType, item.ItemId, item.Count, item.Time }; }
        private static string Csv(string value) { return "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\""; }
        private static string Row(IEnumerable<string> values) { return "<Row>" + string.Join("", values.Select(value => "<Cell><Data ss:Type=\"String\">" + SecurityElement.Escape(value ?? string.Empty) + "</Data></Cell>")) + "</Row>"; }
    }
}
