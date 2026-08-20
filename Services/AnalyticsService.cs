using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GachaLinkFetcher.Models;

namespace GachaLinkFetcher.Services
{
    internal sealed class AnalyticsService
    {
        public string BuildSummary(IEnumerable<GachaRecord> records)
        {
            var all = records.ToList(); if (all.Count == 0) return "暂无本地记录。请先获取链接并点击“同步记录”。";
            GameKind game; Enum.TryParse(all[0].Game, out game);
            var zzz = game == GameKind.ZenlessZoneZero;
            var topRank = zzz ? "4" : "5"; var middleRank = zzz ? "3" : "4"; var lowRank = zzz ? "2" : "3";
            var topName = zzz ? "S 级" : "5 星"; var middleName = zzz ? "A 级" : "4 星"; var lowName = zzz ? "B 级" : "3 星";
            var result = new StringBuilder("共 " + all.Count + " 抽；" + topName + " " + all.Count(item => item.RankType == topRank) + "，" + middleName + " " + all.Count(item => item.RankType == middleRank) + "，" + lowName + " " + all.Count(item => item.RankType == lowRank) + "。\r\n");
            foreach (var group in all.GroupBy(GachaPoolCatalog.CanonicalCode).OrderBy(item => item.Key))
            {
                var ordered = OrderChronologically(group).ToList(); var top = ordered.Select((item, index) => new { item, index }).Where(item => item.item.RankType == topRank).ToList();
                var currentPity = ordered.AsEnumerable().Reverse().TakeWhile(item => item.RankType != topRank).Count();
                var gaps = top.Skip(1).Select((item, index) => item.index - top[index].index).ToList();
                result.Append(GachaPoolCatalog.NameFor(game, group.Key)).Append("：").Append(group.Count()).Append(" 抽，当前垫 ").Append(currentPity).Append("；");
                result.Append(gaps.Count == 0 ? "尚无两次 " + topName + "可计算平均间隔" : topName + "平均间隔 " + gaps.Average().ToString("0.0")).Append("。\r\n");
            }
            return result.ToString().TrimEnd();
        }

        private static IOrderedEnumerable<GachaRecord> OrderChronologically(IEnumerable<GachaRecord> records)
        {
            return records.OrderBy(item => item.Time ?? string.Empty, StringComparer.Ordinal).ThenBy(item => NumericSortKey(item.Id), StringComparer.Ordinal);
        }

        private static string NumericSortKey(string value)
        {
            value = value ?? string.Empty;
            return value.Length > 0 && value.All(char.IsDigit) ? value.Length.ToString("D6") + ":" + value : "999999:" + value;
        }
    }
}
