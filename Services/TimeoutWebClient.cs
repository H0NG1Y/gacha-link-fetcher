using System;
using System.Net;

namespace GachaLinkFetcher.Services
{
    internal sealed class TimeoutWebClient : WebClient
    {
        private readonly int timeoutMilliseconds;

        public TimeoutWebClient(int timeoutMilliseconds)
        {
            if (timeoutMilliseconds <= 0) throw new ArgumentOutOfRangeException("timeoutMilliseconds");
            this.timeoutMilliseconds = timeoutMilliseconds;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address);
            if (request == null) return null;
            request.Timeout = timeoutMilliseconds;
            var httpRequest = request as HttpWebRequest;
            if (httpRequest != null) httpRequest.ReadWriteTimeout = timeoutMilliseconds;
            return request;
        }
    }
}
