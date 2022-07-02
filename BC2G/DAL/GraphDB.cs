using BC2G.Blockchains;
using BC2G.DAL.Bulkload;
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
        private const int _maxEdgesInCSV = 50000;
        private int _edgesInCsvCount;

        private readonly BlockMapper _blockMapper;
        private readonly ScriptMapper _scriptMapper;
        private readonly CoinbaseMapper _coinbaseMapper;

        ~GraphDB() => Dispose(false);

        public GraphDB(
            string uri, 
            string user, 
            string password, 
            string neo4jImportDirectory, 
            string neo4jCypherImportPrefix)
        {
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));

            try { _driver.VerifyConnectivityAsync().Wait(); }
            catch (AggregateException) { Dispose(true); throw; }
            // TODO: it seems message of an exception cannot be modified without impacting 
            // stacktrace. Check if there is a better way of throwing an error with a message
            // that indicates not able to connect to the Neo4j database.

            _blockMapper = new BlockMapper(neo4jCypherImportPrefix, neo4jImportDirectory);
            _scriptMapper = new ScriptMapper(neo4jCypherImportPrefix, neo4jImportDirectory);
            _coinbaseMapper = new CoinbaseMapper(neo4jCypherImportPrefix, neo4jImportDirectory);

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

            if (_edgesInCsvCount != 0 && _edgesInCsvCount + edges.Count >= _maxEdgesInCSV)
                BulkImportStagedAndReset();

            if (_edgesInCsvCount == 0)
            {
                using var bWriter = new StreamWriter(_blockMapper.Filename);
                bWriter.WriteLine(_blockMapper.GetCsvHeader());

                using var eWriter = new StreamWriter(_scriptMapper.Filename);
                eWriter.WriteLine(_scriptMapper.GetCsvHeader());

                using var cWriter = new StreamWriter(_coinbaseMapper.Filename);
                cWriter.WriteLine(_coinbaseMapper.GetCsvHeader());
            }

            using var blocksWriter = new StreamWriter(_blockMapper.Filename, append: true);
            blocksWriter.WriteLine(_blockMapper.ToCsv(graph.Block));

            using var edgesWriter = new StreamWriter(_scriptMapper.Filename, append: true);
            using var coinbaseWrite = new StreamWriter(_coinbaseMapper.Filename, append: true);
            foreach (var edge in edges)
                if (edge.Source.Address == Coinbase)
                    coinbaseWrite.WriteLine(_coinbaseMapper.ToCsv(edge));
                else
                    edgesWriter.WriteLine(_scriptMapper.ToCsv(edge));

            _edgesInCsvCount += edges.Count;
        }

        private void BulkImportStagedAndReset()
        {
            BulkImport();
            File.Delete(_scriptMapper.Filename);
            File.Delete(_blockMapper.Filename);
            File.Delete(_coinbaseMapper.Filename);
            _edgesInCsvCount = 0;
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
                var result = await x.RunAsync(_blockMapper.CypherQuery);
                return result.ToListAsync();
            });
            blockBulkLoadResult.Result.Wait();

            var edgeBulkLoadResult = session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(_scriptMapper.CypherQuery);
                return result.SingleAsync().Result[0].As<string>();
            });
            edgeBulkLoadResult.Wait();

            var coinbaseEdgeBulkLoadResult = session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(_coinbaseMapper.CypherQuery);
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
                    var result = await tx.RunAsync($"MATCH (n:{BitcoinAgent.coinbase}) RETURN COUNT(n)");
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
                            await tx.RunAsync(
                                $"CREATE (:{BitcoinAgent.coinbase} {{" +
                                $"{ScriptMapper.Props[Prop.ScriptAddress].Name}: " +
                                $"\"{BitcoinAgent.coinbase}\"}})");
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
                    "CREATE CONSTRAINT UniqueAddressContraint " +
                    $"FOR (script:{ScriptMapper.labels}) " +
                    $"REQUIRE script.{ScriptMapper.Props[Prop.ScriptAddress].Name} IS UNIQUE");

                return result.ToListAsync();
            });

            var indexAddress = await session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"CREATE INDEX FOR (script:{ScriptMapper.labels}) " +
                    $"ON (script.{ScriptMapper.Props[Prop.ScriptAddress].Name})");
                return result.ToListAsync();
            });

            var indexBlockHeight = await session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"CREATE INDEX FOR (block:{BlockMapper.label})" +
                    $" on (block.{BlockMapper.Props[Prop.Height].Name})");
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

