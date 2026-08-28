namespace GachaLinkFetcher.Models
{
    internal sealed class AppSettings
    {
        public string DownloadRoute { get; set; }
        public string CustomAccelerationUrl { get; set; }
        public string SelectedNode { get; set; }

        public AppSettings()
        {
            DownloadRoute = DownloadRouteKind.Direct.ToString();
            CustomAccelerationUrl = string.Empty;
            SelectedNode = string.Empty;
        }
    }

    internal enum DownloadRouteKind
    {
        Direct,
        Auto,
        Manual,
        Custom
    }

    internal sealed class DownloadRoute
    {
        public DownloadRouteKind Kind { get; set; }
        public string Value { get; set; }

        public DownloadRoute()
        {
            Kind = DownloadRouteKind.Direct;
            Value = string.Empty;
        }
    }
}
