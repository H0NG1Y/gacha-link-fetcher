using System;
using System.Collections.Generic;
using System.Linq;
using GachaLinkFetcher.Models;

namespace GachaLinkFetcher.Services
{
    internal static class GachaPoolCatalog
    {
        private static readonly Dictionary<GameKind, GachaPoolDefinition[]> Pools = new Dictionary<GameKind, GachaPoolDefinition[]>
        {
            {
                GameKind.WutheringWaves,
                new[]
                {
                    Pool("1", "角色活动唤取"), Pool("2", "武器活动唤取"),
                    Pool("8", "角色新旅唤取"), Pool("9", "武器新旅唤取"),
                    Pool("10", "角色联动唤取"), Pool("11", "武器联动唤取"),
                    Pool("12", "角色回响唤取"), Pool("13", "武器回响唤取"),
                    Pool("3", "角色常驻唤取"), Pool("4", "武器常驻唤取"),
                    Pool("5", "新手唤取"), Pool("6", "新手自选唤取"), Pool("7", "感恩定向唤取")
                }
            },
            {
                GameKind.GenshinImpact,
                new[]
                {
                    Pool("301", "角色活动祈愿"), Pool("302", "武器活动祈愿"), Pool("500", "集录祈愿"),
                    Pool("200", "常驻祈愿"), Pool("100", "新手祈愿")
                }
            },
            {
                GameKind.HonkaiStarRail,
                new[]
                {
                    Pool("11", "角色活动跃迁"), Pool("12", "光锥活动跃迁"),
                    Pool("21", "角色联动跃迁"), Pool("22", "光锥联动跃迁"),
                    Pool("1", "常驻跃迁"), Pool("2", "新手跃迁")
                }
            },
            {
                GameKind.ZenlessZoneZero,
                new[]
                {
                    Pool("2", "独家频段"), Pool("3", "音擎频段"), Pool("5", "邦布频段"), Pool("1", "常驻频段")
                }
            }
        };

        private static readonly Dictionary<GameKind, GachaPoolDefinition[]> ApiPools = new Dictionary<GameKind, GachaPoolDefinition[]>
        {
            {
                GameKind.GenshinImpact,
                new[]
                {
                    Pool("301", "角色活动祈愿"), Pool("302", "武器活动祈愿"), Pool("500", "集录祈愿"),
                    Pool("200", "常驻祈愿"), Pool("100", "新手祈愿")
                }
            },
            {
                GameKind.ZenlessZoneZero,
                new[]
                {
                    Pool("2", "独家频段"), Pool("102", "独家频段"),
                    Pool("3", "音擎频段"), Pool("103", "音擎频段"),
                    Pool("5", "邦布频段"), Pool("1", "常驻频段")
                }
            }
        };

        private static readonly Dictionary<GameKind, Dictionary<string, string>> CanonicalCodes = new Dictionary<GameKind, Dictionary<string, string>>
        {
            { GameKind.GenshinImpact, new Dictionary<string, string>(StringComparer.Ordinal) { { "400", "301" } } },
            { GameKind.ZenlessZoneZero, new Dictionary<string, string>(StringComparer.Ordinal) { { "102", "2" }, { "103", "3" } } }
        };

        public static GachaPoolDefinition[] ForGame(GameKind game)
        {
            GachaPoolDefinition[] pools;
            return Pools.TryGetValue(game, out pools) ? pools : new GachaPoolDefinition[0];
        }

        public static GachaPoolDefinition[] ForApi(GameKind game)
        {
            GachaPoolDefinition[] pools;
            return ApiPools.TryGetValue(game, out pools) ? pools : ForGame(game);
        }

        public static string CanonicalCode(GameKind game, string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return code;
            Dictionary<string, string> aliases; string canonical;
            return CanonicalCodes.TryGetValue(game, out aliases) && aliases.TryGetValue(code, out canonical) ? canonical : code;
        }

        public static string NameFor(GameKind game, string code)
        {
            var canonical = CanonicalCode(game, code);
            var pool = ForGame(game).FirstOrDefault(item => string.Equals(item.Code, canonical, StringComparison.Ordinal));
            return pool == null ? "未知卡池（类型 " + (code ?? "") + "）" : pool.Name;
        }

        public static string CanonicalCode(GachaRecord record)
        {
            GameKind game;
            return record != null && Enum.TryParse(record.Game, out game) ? CanonicalCode(game, record.GachaType) : record == null ? null : record.GachaType;
        }

        public static string NameFor(GachaRecord record)
        {
            GameKind game;
            return Enum.TryParse(record.Game, out game) ? NameFor(game, record.GachaType) : record.GachaType;
        }

        private static GachaPoolDefinition Pool(string code, string name) { return new GachaPoolDefinition(code, name); }
    }
}
