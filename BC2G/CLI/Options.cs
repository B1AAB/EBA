namespace BC2G.CLI
{
    public class Options
    {
        public int FromInclusive { get; set; }
        public int LastProcessedBlock { get; set; } = -1;
        public int ToExclusive { get; set; }
        public string OutputDir { get; set; } = string.Empty;
        public string AddressIdMappingFilename { set; get; } = string.Empty;
    }
}
