namespace GachaLinkFetcher.Models
{
    internal sealed class GachaPoolDefinition
    {
        public string Code { get; private set; }
        public string Name { get; private set; }

        public GachaPoolDefinition(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public override string ToString() { return Name; }
    }
}
