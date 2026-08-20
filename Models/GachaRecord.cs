using System;

namespace GachaLinkFetcher.Models
{
    public sealed class GachaRecord
    {
        public string Game { get; set; }
        public string Uid { get; set; }
        public string Id { get; set; }
        public string GachaType { get; set; }
        public string ApiGachaType { get; set; }
        public string Name { get; set; }
        public string ItemType { get; set; }
        public string RankType { get; set; }
        public string ItemId { get; set; }
        public string Count { get; set; }
        public string Time { get; set; }
        public DateTime SyncedAt { get; set; }
        public string Key { get { return Game + "|" + Uid + "|" + GachaType + "|" + (string.IsNullOrWhiteSpace(Id) ? Time + "|" + Name : Id); } }
    }
}
