namespace BC2G.CLI
{
    public class Options
    {
        public int FromInclusive { get; set; } = -1;
        public int ToExclusive { get; set; } = -1;
        public int LastProcessedBlock
        {
            get
            {
                if (_lastProcessedBlock == -1)
                    return FromInclusive == -1 ? -1 : FromInclusive - 1;
                else
                    return _lastProcessedBlock;
            }
            set
            {
                _lastProcessedBlock = value;
            }
        }
        private int _lastProcessedBlock = -1;

        public int Granularity
        {
            get { return _granularity; }
            set { _granularity = value > 0 ? value : 1; }
        }
        private int _granularity = 1;

        public string OutputDir { get; set; } = Environment.CurrentDirectory;
        public string AddressIdMappingFilename { set; get; } = "id_to_address_mapping.tsv";
        public bool CreatePerBlockFiles { get; set; } = false;
        public int MaxConcurrentBlocks { get; set; } = Environment.ProcessorCount / 2;

        public string Neo4jUri { get; set; } =
            Environment.GetEnvironmentVariable("NEO4J_URI") ??
            "bolt://localhost:7687";

        public string Neo4jUser { get; set; } =
            Environment.GetEnvironmentVariable("NEO4J_USER") ??
            "neo4j";
        public string Neo4jPassword { set; get; } =
            Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ??
            "password";
    }
}
