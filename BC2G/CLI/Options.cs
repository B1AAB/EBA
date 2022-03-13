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

        public string OutputDir { get; set; } = Environment.CurrentDirectory;
        public string AddressIdMappingFilename { set; get; } = "address_id_mapping.csv";
        public bool CreatePerBlockFiles { get; set; } = false;
        public int MaxConcurrentBlocks { get; set; } = Environment.ProcessorCount / 2;
    }
}
