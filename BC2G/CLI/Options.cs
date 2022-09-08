using BC2G.DAL;

namespace BC2G.CLI
{
    // TODO: decide on where is a better place to set the
    // defaults and set all the defaults in one place.
    // Currently defaults are set in different places
    // including here and CLI. 

    // TODO: can this class be static? That could
    // simplify setting default values of command line options. 

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

        public bool SkipLoadGraph { get; set; }

        public string WorkingDir { set; get; } = Environment.CurrentDirectory;
        public int GraphSampleCount { set; get; }
        public int GraphSampleHops { set; get; }
        public GraphSampleMode GraphSampleMode { set; get; } = GraphSampleMode.A;
        public int GraphSampleMinNodeCount { set; get; } = 3;
        public int GraphSampleMaxNodeCount { set; get; } = 200;
        public int GraphSampleMinEdgeCount { set; get; } = 3;
        public int GraphSampleMaxEdgeCount { set; get; } = 200;
        public double GraphSampleRootNodeSelectProb
        {
            set
            {
                if (value < 0 || value > 1)
                    _graphSampleRootNodeSelectProb = 1;
                else
                    _graphSampleRootNodeSelectProb = value;
            }
            get { return _graphSampleRootNodeSelectProb; }
        }
        private double _graphSampleRootNodeSelectProb = 0.1;

        public string AddressIdMappingFilename { set; get; } = "id_to_address_mapping.tsv";
        //public bool CreatePerBlockFiles { get; set; } = false;

        public Uri BitcoinClientUri { set; get; } = new Uri("http://127.0.0.1:8332/");


        public int MaxConcurrentBlocks { get; set; }
# if (DEBUG)
            = 1;
#elif (RELEASE)
            = Environment.ProcessorCount / 2;
#endif

        // TODO: Find a better place to define the Neo4j and PostgreSQL related configurations. 

        public string Neo4jUri { get; set; } =
            Environment.GetEnvironmentVariable("NEO4J_URI") ??
            "bolt://localhost:7687";

        public string Neo4jUser { get; set; } =
            Environment.GetEnvironmentVariable("NEO4J_USER") ??
            "neo4j";
        public string Neo4jPassword { set; get; } =
            Environment.GetEnvironmentVariable("NEO4J_PASSWORD") ??
            "password";

        public string Neo4jImportDirectory { set; get; } =
            Environment.GetEnvironmentVariable("NEO4J_IMPORTDIRECTORY") ??
            @"\dbmss\dbms-767f0184-55bd-4192-be0e-294458084cc8\import";

        public string Neo4jCypherImportPrefix { set; get; } =
            Environment.GetEnvironmentVariable("NEO4J_CYPHERIMPORTPREFIX") ??
            "file:///";


        // TODO: 
        // Store these properties and probably the above on the Neo4j in a better way, e.g.,
        // https://docs.microsoft.com/en-us/ef/core/miscellaneous/connection-strings
        public string PsqlHost { set; get; } = "localhost";
        public string PsqlDatabase { set; get; } = "BC2G";
        public string PsqlUsername { set; get; } = "postgres";
        public string PsqlPassword { set; get; } = "PassWord";

        public string StatusFile { set; get; } = Path.Combine(Environment.CurrentDirectory, "status.json");

        public TimeSpan HttpRequestTimeout { set; get; } = new TimeSpan(0, 5, 0);
    }
}
