using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using GachaLinkFetcher.Models;

namespace GachaLinkFetcher.Services
{
    internal sealed class GameApiClient
    {
        private const int HoyoversePageDelayMilliseconds = 300;
        private const int HoyoversePoolDelayMilliseconds = 1000;
        private const int RateLimitRetryDelayMilliseconds = 5000;
        private const int RateLimitRetryCount = 5;
        private readonly JavaScriptSerializer serializer = new JavaScriptSerializer { MaxJsonLength = 67108864 };

        public SyncResult Download(GameKind game, string recordLink)
        {
            if (string.IsNullOrWhiteSpace(recordLink)) throw new InvalidOperationException("请先获取抽卡链接。");
            return game == GameKind.WutheringWaves ? DownloadWuthering(recordLink) : DownloadHoyoverse(game, recordLink);
        }

        private SyncResult DownloadHoyoverse(GameKind game, string link)
        {
            var query = ParseQuery(link);
            if (!query.ContainsKey("authkey")) throw new InvalidOperationException("链接中没有找到有效的 authkey。请重新打开游戏内记录页后再获取链接。");

            var result = new SyncResult();
            var baseEndpoint = EndpointFor(game, link);
            foreach (var pool in GachaPoolCatalog.ForApi(game))
            {
                var endpoint = EndpointForPool(game, baseEndpoint, pool.Code);
                result.Records.AddRange(DownloadHoyoversePool(game, endpoint, query, pool));
                Thread.Sleep(HoyoversePoolDelayMilliseconds);
            }

            if (result.Records.Count == 0)
                throw new InvalidOperationException("官方接口未返回记录。链接可能已过期，或当前账号在可查询期限内没有记录；请重新打开记录页后再试。");

            var poolCount = result.Records.Select(item => item.GachaType).Distinct().Count();
            result.Message = "已获取全部可用记录 " + result.Records.Count + " 条，覆盖 " + poolCount + " 个卡池。";
            return result;
        }

        private IEnumerable<GachaRecord> DownloadHoyoversePool(GameKind game, string endpoint, Dictionary<string, string> original, GachaPoolDefinition pool)
        {
            var records = new List<GachaRecord>();
            var seenCursors = new HashSet<string>(StringComparer.Ordinal);
            var endId = string.Empty;
            var page = 1;

            while (true)
            {
                var query = new Dictionary<string, string>(original, StringComparer.OrdinalIgnoreCase);
                query.Remove("gacha_type"); query.Remove("real_gacha_type"); query.Remove("page"); query.Remove("size"); query.Remove("end_id");
                query[game == GameKind.ZenlessZoneZero ? "real_gacha_type" : "gacha_type"] = pool.Code;
                query["page"] = page.ToString(); query["size"] = "20";
                if (!string.IsNullOrWhiteSpace(endId)) query["end_id"] = endId;

                var requestUrl = endpoint + "?" + BuildQuery(query);
                var root = GetHoyoversePage(requestUrl, endpoint, pool.Name);
                var list = GetList(GetObject(root, "data"), "list");
                if (list.Count == 0) break;

                foreach (var entry in list) records.Add(ToHoyoverseRecord(game, entry, pool.Code));
                var nextCursor = Value(list[list.Count - 1], "id");
                if (string.IsNullOrWhiteSpace(nextCursor) || !seenCursors.Add(nextCursor)) break;

                endId = nextCursor; page++; Thread.Sleep(HoyoversePageDelayMilliseconds);
            }
            return records;
        }

        private Dictionary<string, object> GetHoyoversePage(string requestUrl, string referer, string poolName)
        {
            for (var retry = 0; ; retry++)
            {
                var root = JsonObject(Get(requestUrl, referer));
                int code;
                var hasCode = int.TryParse(Value(root, "retcode") ?? Value(root, "code"), out code);
                if (!hasCode || code == 0) return root;
                if (code != -110 || retry >= RateLimitRetryCount)
                {
                    EnsureApiSuccess(root, poolName);
                    return root;
                }

                Thread.Sleep(RateLimitRetryDelayMilliseconds);
            }
        }

        private SyncResult DownloadWuthering(string link)
        {
            var query = ParseQuery(link);
            var playerId = First(query, "player_id", "role_id", "uid");
            var cardPoolId = First(query, "resources_id", "resource_id");
            var recordId = First(query, "record_id");
            var serverId = First(query, "svr_id", "server_id");
            var language = First(query, "lang", "language") ?? "zh-Hans";
            var serviceArea = First(query, "svr_area", "service_area") ?? (link.IndexOf("oversea", StringComparison.OrdinalIgnoreCase) >= 0 ? "oversea" : "cn");

            if (string.IsNullOrWhiteSpace(playerId) || string.IsNullOrWhiteSpace(cardPoolId) || string.IsNullOrWhiteSpace(recordId) || string.IsNullOrWhiteSpace(serverId))
                throw new InvalidOperationException("鸣潮记录链接缺少 player_id、resources_id、record_id 或 svr_id。请重新进入游戏内唤取记录后再获取链接。");

            var overseas = !string.Equals(serviceArea, "cn", StringComparison.OrdinalIgnoreCase);
            var endpoint = overseas ? "https://gmserver-api.aki-game2.net/gacha/record/query" : "https://gmserver-api.aki-game2.com/gacha/record/query";
            var referer = overseas ? "https://aki-gm-resources-oversea.aki-game.net/" : "https://aki-gm-resources.aki-game.com/";
            var result = new SyncResult();

            foreach (var pool in GachaPoolCatalog.ForApi(GameKind.WutheringWaves))
            {
                var payload = new Dictionary<string, object>
                {
                    { "cardPoolId", cardPoolId }, { "cardPoolType", int.Parse(pool.Code) },
                    { "languageCode", language }, { "playerId", playerId },
                    { "recordId", recordId }, { "serverId", serverId }
                };
                var root = JsonObject(PostJson(endpoint, referer, payload));
                EnsureApiSuccess(root, pool.Name);
                result.Records.AddRange(ToWutheringRecords(GetList(root, "data"), pool.Code, playerId));
                Thread.Sleep(250);
            }

            if (result.Records.Count == 0)
                throw new InvalidOperationException("鸣潮官方接口未返回记录。请重新打开唤取记录页后获取新链接再试。");

            var poolCount = result.Records.Select(item => item.GachaType).Distinct().Count();
            result.Message = "已获取全部可用记录 " + result.Records.Count + " 条，覆盖 " + poolCount + " 个卡池。";
            return result;
        }

        private static IEnumerable<GachaRecord> ToWutheringRecords(IEnumerable<Dictionary<string, object>> list, string poolType, string playerId)
        {
            var records = new List<GachaRecord>();
            var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var item in list)
            {
                var time = Value(item, "time") ?? Value(item, "create_time") ?? string.Empty;
                var name = Value(item, "name") ?? Value(item, "resource_name") ?? string.Empty;
                var itemType = Value(item, "resourceType") ?? Value(item, "resource_type") ?? string.Empty;
                var rank = Value(item, "qualityLevel") ?? Value(item, "quality_level") ?? string.Empty;
                var itemId = Value(item, "resourceId") ?? Value(item, "resource_id") ?? string.Empty;
                var count = Value(item, "count") ?? "1";
                var signature = poolType + "|" + time + "|" + name + "|" + itemType + "|" + rank + "|" + itemId + "|" + count;
                int occurrence; occurrences.TryGetValue(signature, out occurrence); occurrence++; occurrences[signature] = occurrence;
                records.Add(new GachaRecord
                {
                    Game = GameKind.WutheringWaves.ToString(), Uid = playerId,
                    Id = "ww-" + StableHash(signature) + "-" + occurrence,
                    GachaType = poolType, ApiGachaType = poolType,
                    Name = name, ItemType = itemType, RankType = rank,
                    ItemId = itemId, Count = count, Time = time, SyncedAt = DateTime.Now
                });
            }
            return records;
        }

        private static GachaRecord ToHoyoverseRecord(GameKind game, Dictionary<string, object> item, string requestedType)
        {
            var apiType = game == GameKind.ZenlessZoneZero
                ? Value(item, "real_gacha_type") ?? requestedType
                : Value(item, "gacha_type") ?? requestedType;
            return new GachaRecord
            {
                Game = game.ToString(), Uid = FirstValue(item, "uid", "role_id") ?? string.Empty,
                Id = Value(item, "id"), GachaType = GachaPoolCatalog.CanonicalCode(game, requestedType),
                ApiGachaType = apiType,
                Name = Value(item, "name"), ItemType = Value(item, "item_type"),
                RankType = Value(item, "rank_type"), ItemId = Value(item, "item_id"),
                Count = Value(item, "count") ?? "1", Time = Value(item, "time"), SyncedAt = DateTime.Now
            };
        }

        private string Get(string url, string referer)
        {
            EnsureTls12(); var request = CreateRequest(url, referer); request.Method = "GET"; return ReadResponse(request);
        }

        private string PostJson(string url, string referer, object payload)
        {
            EnsureTls12();
            var request = CreateRequest(url, referer); request.Method = "POST"; request.ContentType = "application/json"; request.Accept = "application/json";
            var bytes = Encoding.UTF8.GetBytes(serializer.Serialize(payload)); request.ContentLength = bytes.Length;
            try { using (var stream = request.GetRequestStream()) stream.Write(bytes, 0, bytes.Length); return ReadResponse(request); }
            catch (WebException ex) { throw WebError(ex); }
        }

        private static HttpWebRequest CreateRequest(string url, string referer)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Timeout = 20000; request.ReadWriteTimeout = 20000;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) GachaLinkFetcher/3.0";
            request.Referer = referer; return request;
        }

        private static string ReadResponse(HttpWebRequest request)
        {
            try { using (var response = (HttpWebResponse)request.GetResponse()) using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8)) return reader.ReadToEnd(); }
            catch (WebException ex) { throw WebError(ex); }
        }

        private static InvalidOperationException WebError(WebException ex)
        {
            var response = ex.Response as HttpWebResponse;
            return new InvalidOperationException(response == null ? "无法连接官方记录接口：" + ex.Message : "官方记录接口返回 HTTP " + (int)response.StatusCode + "。请重新获取链接后重试。");
        }

        private static void EnsureTls12() { ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072; }

        private static string EndpointFor(GameKind game, string link)
        {
            var overseas = link.IndexOf("hoyoverse.com", StringComparison.OrdinalIgnoreCase) >= 0 || link.IndexOf("_global", StringComparison.OrdinalIgnoreCase) >= 0;
            if (game == GameKind.GenshinImpact) return overseas ? "https://public-operation-hk4e-sg.hoyoverse.com/gacha_info/api/getGachaLog" : "https://public-operation-hk4e.mihoyo.com/gacha_info/api/getGachaLog";
            if (game == GameKind.HonkaiStarRail) return overseas ? "https://public-operation-hkrpg-sg.hoyoverse.com/common/gacha_record/api/getGachaLog" : "https://public-operation-hkrpg.mihoyo.com/common/gacha_record/api/getGachaLog";
            return overseas ? "https://public-operation-nap-sg.hoyoverse.com/common/gacha_record/api/getGachaLog" : "https://public-operation-nap.mihoyo.com/common/gacha_record/api/getGachaLog";
        }

        private static string EndpointForPool(GameKind game, string endpoint, string poolType)
        {
            return game == GameKind.HonkaiStarRail && (poolType == "21" || poolType == "22") ? endpoint.Replace("getGachaLog", "getLdGachaLog") : endpoint;
        }

        private Dictionary<string, object> JsonObject(string json)
        {
            try { return serializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>(); }
            catch (Exception) { throw new InvalidOperationException("官方接口响应格式无法解析，游戏接口可能已经更新。"); }
        }

        private static void EnsureApiSuccess(Dictionary<string, object> root, string poolName)
        {
            var codeText = Value(root, "retcode") ?? Value(root, "code"); int code;
            if (int.TryParse(codeText, out code) && code != 0)
            {
                var message = Value(root, "message") ?? Value(root, "msg") ?? "请求失败";
                if (code == -110)
                    throw new InvalidOperationException(poolName + "：官方接口访问过于频繁（代码 -110），自动限速重试后仍未恢复。请等待约 1 分钟后再试。");
                throw new InvalidOperationException(poolName + "：" + message + "（代码 " + code + "）。请重新获取链接后重试。");
            }
        }

        private static Dictionary<string, object> GetObject(Dictionary<string, object> source, string key)
        {
            object value; return source != null && source.TryGetValue(key, out value) ? value as Dictionary<string, object> : null;
        }

        private static List<Dictionary<string, object>> GetList(Dictionary<string, object> source, string key)
        {
            object value; var output = new List<Dictionary<string, object>>();
            if (source == null || !source.TryGetValue(key, out value) || value == null) return output;
            var values = value as IEnumerable; if (values == null) return output;
            foreach (var item in values) { var map = item as Dictionary<string, object>; if (map != null) output.Add(map); }
            return output;
        }

        private static string Value(Dictionary<string, object> item, string key)
        {
            object value; return item != null && item.TryGetValue(key, out value) && value != null ? Convert.ToString(value) : null;
        }

        private static string FirstValue(Dictionary<string, object> item, params string[] keys)
        {
            foreach (var key in keys) { var value = Value(item, key); if (!string.IsNullOrWhiteSpace(value)) return value; } return null;
        }

        private static string First(Dictionary<string, string> values, params string[] keys)
        {
            foreach (var key in keys) { string value; if (values.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value)) return value; } return null;
        }

        private static Dictionary<string, string> ParseQuery(string link)
        {
            var query = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); var marker = link.IndexOf('?'); if (marker < 0) return query;
            var value = link.Substring(marker + 1); var hash = value.IndexOf('#'); if (hash >= 0) value = value.Substring(0, hash);
            foreach (var part in value.Split('&')) { var pair = part.Split(new[] { '=' }, 2); if (pair.Length == 2) query[Uri.UnescapeDataString(pair[0])] = Uri.UnescapeDataString(pair[1]); }
            return query;
        }

        private static string BuildQuery(Dictionary<string, string> values)
        {
            return string.Join("&", values.Where(item => !string.IsNullOrWhiteSpace(item.Value)).Select(item => Uri.EscapeDataString(item.Key) + "=" + Uri.EscapeDataString(item.Value)));
        }

        private static string StableHash(string value)
        {
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value)); var output = new StringBuilder();
                for (var index = 0; index < 10; index++) output.Append(hash[index].ToString("x2")); return output.ToString();
            }
        }
    }
}
