namespace GachaLinkFetcher.Models
{
    internal sealed class GameDefinition
    {
        public GameKind Kind; public string Name; public string RecordName; public string DataFolder; public string Marker; public string[] Roots;
        public GameDefinition(GameKind kind, string name, string recordName, string dataFolder, string marker, params string[] roots)
        { Kind = kind; Name = name; RecordName = recordName; DataFolder = dataFolder; Marker = marker; Roots = roots; }
    }
}
