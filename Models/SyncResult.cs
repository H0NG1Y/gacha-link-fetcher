using System.Collections.Generic;

namespace GachaLinkFetcher.Models
{
    internal sealed class SyncResult
    {
        public List<GachaRecord> Records = new List<GachaRecord>();
        public string Message;
    }
}
