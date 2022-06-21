using BC2G.Graph;
using BC2G.Model;
using Neo4j.Driver;

namespace BC2G.DAL
{
    public class GraphDB : IDisposable
    {
        public static string Coinbase { get { return "Coinbase"; } }

        private bool _disposed = false;
        private readonly IDriver _driver;

        /// <summary>
        /// Neo4j docs suggest between 10,000 and 100,000 updates 
        /// per transaction as a good target. 
        /// 
        /// Ref: https://neo4j.com/blog/bulk-data-import-neo4j-3-0/
        /// </summary>
        private const int _maxEdgesInCSV = 2;//50000;
        private int _edgesInCSV;

        private readonly BlockBulkLoadMapper _blockMapper = new();
        private readonly EdgeBlockBulkMapper _edgeMapper = new();
        private readonly CoinbaseTxBulkLoadMapper _coinbaseMapper = new();

        private const string _delimiter = "\t";
        private readonly string _coinbaseCSVHeader = string.Join(_delimiter, new string[]
        {
            "TargetScriptType", "TargetAddress",
            "Type", "Value", "BlockHeight"
        });

        private readonly string _neo4jImportDir;
        private const string _edgesCSVFilename = "tmpBulkImportEdges.csv";
        private readonly string _edgesCSVAbsFilename;
        private const string _coinbaseEdgesCSVFilename = "tmpCoinbase.csv";
        private readonly string _coinbaseEdgesCSVAbsFilename;
        private const string _blocksCSVFilename = "tmpBulkImportBlocks.csv";
        private readonly string _blocksCSVAbsFilename;

        private static readonly NodeMapping _nodeMapping = new()
        {
            Labels = new List<string>() { "Script" },
            PropertyMappings = new Dictionary<string, string>()
            {
                { nameof(Node.Id), nameof(Node.Id) }
            }
        };

        ~GraphDB() => Dispose(false);

        public GraphDB(string uri, string user, string password, string importDirectory)
        {
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));

            try { _driver.VerifyConnectivityAsync().Wait(); }
            catch (AggregateException) { Dispose(true); throw; }
            // TODO: it seems message of an exception cannot be modified without impacting 
            // stacktrace. Check if there is a better way of throwing an error with a message
            // that indicates not able to connect to the Neo4j database.

            _neo4jImportDir = importDirectory;
            _edgesCSVAbsFilename = Path.Combine(_neo4jImportDir, _edgesCSVFilename);
            _blocksCSVAbsFilename = Path.Combine(_neo4jImportDir, _blocksCSVFilename);
            _coinbaseEdgesCSVAbsFilename = Path.Combine(_neo4jImportDir, _coinbaseEdgesCSVFilename);

            EnsureCoinbaseNode().Wait();
            EnsureConstraints().Wait();
            /*
            var script = new NodeMapping();
            script.Labels.Add("Script");
            var props = new Node().GetType().GetProperties();

            var x = new Node("abc", "", ScriptType.NullData);
            
            string y = nameof(x.Id);

            var v = x.GetType().GetProperty("Id").GetValue(x);*/
        }

        /* TODO:
         * - create constraints on address and script type so lookup could be much faster. 
         * -- check if constraints already exist or not, and add if missing. 
         * 
         * - When using MERGE or MATCH with LOAD CSV, make sure you have an index or a 
         * unique constraint on the property that you are merging on. This will 
         * ensure that the query executes in a performant way.
         */

        public void BulkImport(BlockGraph graph, CancellationToken cT)
        {
            var edges = graph.Edges;

            if (_edgesInCSV != 0 && _edgesInCSV + edges.Count >= _maxEdgesInCSV)
                BulkImportStagedAndReset();

            if (_edgesInCSV == 0)
            {
                using var bWriter = new StreamWriter(_blocksCSVAbsFilename);
                bWriter.WriteLine(_blockMapper.GetCsvHeader());

                using var eWriter = new StreamWriter(_edgesCSVAbsFilename);
                eWriter.WriteLine(_edgeMapper.GetCsvHeader());

                using var cWriter = new StreamWriter(_coinbaseEdgesCSVAbsFilename);
                cWriter.WriteLine(_coinbaseMapper.GetCsvHeader());
            }

            using var blocksWriter = new StreamWriter(_blocksCSVAbsFilename, append: true);
            blocksWriter.WriteLine(_blockMapper.ToCsv(graph.Block));

            using var edgesWriter = new StreamWriter(_edgesCSVAbsFilename, append: true);
            using var coinbaseWrite = new StreamWriter(_coinbaseEdgesCSVAbsFilename, append: true);
            foreach (var edge in edges)
                if (edge.Source.Address == Coinbase)
                    coinbaseWrite.WriteLine(_coinbaseMapper.ToCsv(edge));
                else
                    edgesWriter.WriteLine(_edgeMapper.ToCsv(edge));

            _edgesInCSV += edges.Count;
        }

        private void BulkImportStagedAndReset()
        {
            BulkImport();
            File.Delete(_edgesCSVAbsFilename);
            File.Delete(_blocksCSVAbsFilename);
            File.Delete(_coinbaseEdgesCSVAbsFilename);
            _edgesInCSV = 0;
        }

        public void FinishBulkImport()
        {
            BulkImportStagedAndReset();
        }

        private void BulkImport()
        {
            using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));

            var blockBulkLoadResult = session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(_blockMapper.GetCypherQuery($"file:///{_blocksCSVFilename}"));
                return result.ToListAsync();
            });
            blockBulkLoadResult.Result.Wait();

            var edgeBulkLoadResult = session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(_edgeMapper.GetCypherQuery($"file:///{_edgesCSVFilename}"));
                return result.ToListAsync();
            });
            edgeBulkLoadResult.Result.Wait();

            var coinbaseEdgeBulkLoadResult = session.WriteTransactionAsync(async x =>
            {
            var result = await x.RunAsync(_coinbaseMapper.GetCypherQuery($"file:///{_coinbaseEdgesCSVFilename}"));
                return result.ToListAsync();
            });
            coinbaseEdgeBulkLoadResult.Result.Wait();
        }

        public async Task TEST_LoadCSV()
        {
            var dataFile = "data.csv";
            var dataFile2 = "data2.csv";
            using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));

            var a0 = await session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"LOAD CSV WITH HEADERS FROM 'file:///{dataFile2}' AS line FIELDTERMINATOR '\t'" +
                    "MERGE (source:Script {scriptType: line.SourceScriptType, address: line.SourceAddress})" +
                    "MERGE (target:Script {scriptType: line.TargetScriptType, address: line.TargetAddress})" +
                    "CREATE (source)-[:line.EdgeType {value: {line.EdgeValue}, block: {line.BlockHeight}}]->(target)");
                return result.ToListAsync();
            });

            var aa = await session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(
                    "CREATE CONSTRAINT personIdConstraint " +
                    "FOR (person:Person) " +
                    "REQUIRE person.id IS UNIQUE");

                return result.ToListAsync();
            });

            var yy = await session.ReadTransactionAsync(async x =>
            {
                var result = await x.RunAsync("SHOW EXISTENCE CONSTRAINTS WHERE name = 'personIdConstraint'");
                return result.ToListAsync();
            });

            await session.WriteTransactionAsync(async tx =>
            {
                var result = await tx.RunAsync(
                    $"LOAD CSV WITH HEADERS FROM 'file:///{dataFile}' AS line FIELDTERMINATOR ','" +
                    "CREATE(:Artist { name: line.Name, year: toInteger(line.Year)})");
            });
            /*
            await session.WriteTransactionAsync(async tx =>
            {
                var result = await tx.RunAsync(
                    "LOAD CSV WITH HEADERS FROM 'file:///cypher_load_csv_test.csv' AS line " +
                    "CREATE(:Artist { name: line.Name, year: toInteger(line.Year)})");
            });*/
        }

        private async Task EnsureCoinbaseNode()
        {
            int count = 0;
            using (var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read)))
            {
                count = await session.ReadTransactionAsync(async tx =>
                {
                    var result = await tx.RunAsync($"MATCH (n:{Coinbase}) RETURN COUNT(n)");
                    return result.SingleAsync().Result[0].As<int>();
                });
            }

            switch (count)
            {
                case 1: return;
                case 0:
                    using (var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write)))
                    {
                        await session.WriteTransactionAsync(async tx =>
                        {
                            await tx.RunAsync($"CREATE (:{Coinbase} {{address: \"{Coinbase}\"}})");
                        });
                    }
                    break;
                default:
                    // TODO: replace with a more suitable exception type. 
                    throw new Exception($"Found {count} {Coinbase} nodes; expected zero or one.");
            }
        }

        private async Task EnsureConstraints()
        {
            using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));


            // TODO: handle the exceptions raised in running the following.
            // Note that the exceptions are stored in the Exceptions property
            // and do not log and stop execution when raised. 
            var addressUniquenessContraint = await session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(
                    "CREATE CONSTRAINT addressUniqueContraint " +
                    "FOR (script:Script) " +
                    "REQUIRE script.address IS UNIQUE");

                return result.ToListAsync();
            });

            var indexAddress = await session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(
                    "CREATE INDEX FOR (script:Script) " +
                    "ON (script.Address)");
                return result.ToListAsync();
            });

            var indexBlockHeight = await session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync("CREATE INDEX FOR (block:Block) on (block.height)");
                return result.ToListAsync();
            });


            // TODO: check if existing contraints as the following, and add contrains only
            // if they are not already defined. Alternatively, try creating the contrains, 
            // and if they already exist, you'll see an Exception (non-blocking) in the
            // above code. 
            /*var xyz = await session.ReadTransactionAsync(async x =>
            {
                var result = await x.RunAsync("CALL db.constraints");
                return result.ToListAsync();
            });*/
        }

        public async Task AddBlock(Block block)
        {
            using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));
            await session.WriteTransactionAsync(async tx =>
            {
                var result = await tx.RunAsync(
                    $"MERGE (x:h{block.Height}:t{block.MedianTime} " +
                    $"{{difficulty: {block.Difficulty}, " +
                    $"confirmations: {block.Confirmations}, " +
                    $"tx_count: {block.TransactionsCount}, " +
                    $"stripped_size: {block.StrippedSize}, " +
                    $"size: {block.Size}, " +
                    $"weight: {block.Weight}}})");
            });
        }

        public async Task AddEdge(Block block, Edge edge)
        {
            using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));
            await session.WriteTransactionAsync(async tx =>
            {
                /*
                var result = await tx.RunAsync(
                    $"MERGE (x:{edge.Source.ScriptType}:_{edge.Source.Address}) " +
                    $"MERGE (y:{edge.Target.ScriptType}:_{edge.Target.Address}) " +
                    $"CREATE (x)-[:{edge.Type} {{value: {edge.Value}, block: {edge.BlockHeight}}}]->(y)");
                */

                var result = await tx.RunAsync(
                    $"MERGE (x:{edge.Source.ScriptType}:_{edge.Source.Address}) " +
                    $"MERGE (y:{edge.Target.ScriptType}:_{edge.Target.Address}) " +
                    $"WITH x, y " +
                    $"MATCH (b:b{block.Height}:t{block.MedianTime}) " +
                    $"CREATE (x)-[:{edge.Type} {{value: {edge.Value}, block: {edge.BlockHeight}}}]->(y) " +
                    $"CREATE (x)-[:Redeems]->(b) " +
                    $"CREATE (b)-[:Creates]->(y)");
            });
        }

        public async Task AddNode(ScriptType scriptType, string address)
        {
            using (var session = _driver.AsyncSession())
            {
                var x = session.WriteTransactionAsync(async tx =>
                {
                    var result = await tx.RunAsync(
                        $"CREATE (n:{scriptType}:_{address}) RETURN id(n)");
                });

                await x;
            }

            using(var session = _driver.AsyncSession())
            {
                var x = session.WriteTransactionAsync(async tx =>
                {
                    var result = await tx.RunAsync(
                        "MATCH (p:abc) RETURN p");

                    return result.SingleAsync().Result;
                });

                await x;

                var z = 10;
            }

            var y = 10;
        }

        public async void PrintGreeting(string message)
        {
            using (var session = _driver.AsyncSession())
            {
                var greeting = session.WriteTransactionAsync(async tx =>
                {
                    var result = await tx.RunAsync("CREATE (a:Greeting) " +
                                        "SET a.message = $message " +
                                        "RETURN a.message + ', from node ' + id(a)",
                        new { message });
                    return result.SingleAsync().Result[0].As<string>();
                });
                var xxx = await greeting;
                Console.WriteLine(await greeting);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _driver?.Dispose();
            }

            _disposed = true;
        }
    }
}
