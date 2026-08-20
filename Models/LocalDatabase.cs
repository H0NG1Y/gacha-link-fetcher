using System.Collections.Generic;

namespace GachaLinkFetcher.Models
{
    public sealed class LocalDatabase
    {
        public List<GachaRecord> Records { get; set; }
        public LocalDatabase() { Records = new List<GachaRecord>(); }
    }
}
