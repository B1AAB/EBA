using BC2G.Blockchains;
using BC2G.CLI;
using BC2G.DAL.Bulkload;
using BC2G.Graph;
using BC2G.Model;
using Neo4j.Driver;

namespace BC2G.DAL
{

    // TODO: there is a bug: why many redeems per node in a given block? 
    // TODO: add time stamp to edge.

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

            // TEMP
            
        }

        /* TODO:
         * - create constraints on address and script type so lookup could be much faster. 
         * -- check if constraints already exist or not, and add if missing. 
         * 
         * - When using MERGE or MATCH with LOAD CSV, make sure you have an index or a 
         * unique constraint on the property that you are merging on. This will 
         * ensure that the query executes in a performant way.
         * 
         * - make sure apoc and all the necessary plug-ins are installed on the given
         * Neo4j database at the time of initialization. Currently if any of the plug 
         * ins are not installed it fails at the load time. 
         */

        public void BulkImport(BlockGraph graph, CancellationToken cT)
        {
            // TODO: Neo4j deadlock can happen in the following calls too. 
            // The error message:
            // 
            // One or more errors occurred. (One or more errors occurred.
            // (ForsetiClient[transactionId=465, clientId=3] can't acquire
            // ExclusiveLock{owner=ForsetiClient[transactionId=463, clientId=1]}
            // on NODE(272254), because holders of that lock are waiting for
            // ForsetiClient[transactionId=465, clientId=3].
            // Wait list:ExclusiveLock[
            // Client[463] waits for [ForsetiClient[transactionId = 465, clientId = 3]]]))

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
            blocksWriter.WriteLine(_blockMapper.ToCsv(graph));

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

            // TODO: in all the awaits in the following, capture deadlock error and retry the statement
            // if the error is occured. See the following:
            // 
            // One or more errors occurred. (One or more errors occurred.
            // (ForsetiClient[transactionId=238, clientId=1] can't acquire
            // ExclusiveLock{owner=ForsetiClient[transactionId=236, clientId=2]}
            // on NODE(264194), because holders of that lock are waiting
            // for ForsetiClient[transactionId=238, clientId=1].
            // Wait list:ExclusiveLock[
            // Client[236] waits for [ForsetiClient[transactionId = 238, clientId = 1]]]))
            // 
            // The type of inner exception is: 
            // "Neo.TransientError.Transaction.DeadlockDetected"

            var blockBulkLoadResult = session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(_blockMapper.CypherQuery);
                return await result.ToListAsync();
            });
            blockBulkLoadResult.Wait();

            var edgeBulkLoadResult = session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(_scriptMapper.CypherQuery);
                return result.SingleAsync().Result[0].As<string>();
            });
            edgeBulkLoadResult.Wait();

            var coinbaseEdgeBulkLoadResult = session.WriteTransactionAsync(async x =>
            {
                var result = await x.RunAsync(_coinbaseMapper.CypherQuery);
                return await result.ToListAsync();
            });
            coinbaseEdgeBulkLoadResult.Wait();

            // File deleted before the above query is finished?!!! 
            // One or more errors occurred. (Couldn't load the external resource at:
            // file://dbmss/dbms-ff193aad-d42a-4cf2-97b5-e7fe6b52b161/import/tmpBulkImportCoinbase.csv)
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

            // TODO: do not create contraints if they already exist,
            // otherwise you'll get the following error: 
            //
            // One or more errors occurred. (An equivalent constraint
            // already exists, 'Constraint( id=4,
            // name='UniqueAddressContraint', type='UNIQUENESS',
            // schema=(:Script {Address}), ownedIndex=3 )'.)

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

        public async Task Sampling(Options options, double rootNodesSelectProb = 0.1)
        {
            var includeRndEdges = true;

            var rndRootNodes = await GetRandomNodes(options.GraphSampleCount, rootNodesSelectProb);

            var baseOutputDir = options.WorkingDir;
            var counter = -1;
            foreach (var rootNode in rndRootNodes)
            {
                var features = new Dictionary<string, (List<double[]>, List<double[]>, List<int[]>, List<int[]>)>();

                (var nodes, var edges) = await GetNeighbors(rootNode.Address, options.GraphSampleHops);

                if (!CanUseGraph(
                    nodes, edges, tolerance: 0, 
                    minNodeCount: options.GraphSampleMinNodeCount, 
                    maxNodeCount: options.GraphSampleMaxNodeCount, 
                    minEdgeCount: options.GraphSampleMinEdgeCount, 
                    maxEdgeCount: options.GraphSampleMaxEdgeCount))
                    continue;

                if (includeRndEdges)
                {
                    (var rnodes, var redges) = await GetRandomEdges(edges.Count);

                    if (!CanUseGraph(rnodes, redges, maxNodeCount: nodes.Count, maxEdgeCount: edges.Count))
                        continue;

                    (var rNodeFeatures, var rEdgeFeatures, var rPairIndices) = ToMatrix(rnodes, redges);
                    features.Add("RandomEdges", (rNodeFeatures, rEdgeFeatures, rPairIndices, new List<int[]> { new int[] { 1 } }));
                }

                (var nodeFeatures, var edgeFeatures, var pairIndices) = ToMatrix(nodes, edges);
                features.Add("Graph", (nodeFeatures, edgeFeatures, pairIndices, new List<int[]> { new int[] { 0 } }));

                counter++;
                foreach (var graphFeatures in features)
                {
                    var outputDir = Path.Join(baseOutputDir, counter.ToString(), graphFeatures.Key);
                    Directory.CreateDirectory(outputDir);

                    (var nFeatures, var eFeatures, var pIndices, var labels) = graphFeatures.Value;
                    ToTSV(nFeatures, Path.Join(outputDir, "node_features.tsv"));
                    ToTSV(eFeatures, Path.Join(outputDir, "edge_features.tsv"));
                    ToTSV(pIndices, Path.Join(outputDir, "pair_indices.tsv"));
                    ToTSV(labels, Path.Join(outputDir, "labels.tsv"));
                }
            }
        }

        private static bool CanUseGraph(
            Dictionary<long, Node> nodes,
            List<IRelationship> edges,
            int minNodeCount = 3, int maxNodeCount = 200,
            int minEdgeCount = 3, int maxEdgeCount = 200,
            double tolerance = 0.5)
        {
            // TODO: implement checks on the graph; e.g., graph size, or if it was already defined.

            // TODO: very big graphs cause various issues
            // with Tensorflow when training, such as out-of-memory
            // (hence radically slow process), or even trying to
            // multiply matrixes of very large size 2**32 or even
            // larger. There should be much better workarounds at
            // Tensorflow level, but for now, we limit the size of graphs.
            if (nodes.Count < minNodeCount - (minNodeCount * tolerance) || nodes.Count > maxNodeCount + (maxNodeCount * tolerance) ||
                edges.Count < minEdgeCount - (minEdgeCount * tolerance) || edges.Count > maxEdgeCount + (maxEdgeCount * tolerance))
                return false;

            return true;
        }

        private void ToTSV<T>(List<T[]> data, string filename)
        {
            using var writer = new StreamWriter(filename);
            foreach (var x in data)
                writer.WriteLine(string.Join("\t", x));
        }

        private async Task<List<Node>> GetRandomNodes(
            int nodesCount, double rootNodesSelectProb = 0.1)
        {
            using var session = _driver.AsyncSession(
                x => x.WithDefaultAccessMode(AccessMode.Read));

            var rndNodesResult = session.ReadTransactionAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"MATCH (rndScript:Script)-[:Transfer]->() " +
                    $"WHERE rand() < {rootNodesSelectProb} " +
                    $"RETURN rndScript LIMIT {nodesCount}");

                return await result.ToListAsync();
            });
            await rndNodesResult;

            var rndNodes = new List<Node>();
            foreach (var n in rndNodesResult.Result)
                rndNodes.Add(ParseNode(n.Values["rndScript"].As<INode>()));

            return rndNodes;
        }

        private async Task<(Dictionary<long, Node>, List<IRelationship>)> GetRandomEdges(int edgeCount, double edgeSelectProb = 0.1)
        {
            using var session = _driver.AsyncSession(
                x => x.WithDefaultAccessMode(AccessMode.Read));

            var rndNodesResult = session.ReadTransactionAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"Match (source)-[edge:Transfer]->(target) " +
                    $"where rand() < {edgeSelectProb} " +
                    $"return source, edge, target  limit {edgeCount}");

                return await result.ToListAsync();
            });
            await rndNodesResult;

            var rndNodes = new Dictionary<long, Node>();
            var rndEdges = new List<IRelationship>();
            foreach (var n in rndNodesResult.Result)
            {
                var isource = n.Values["source"].As<INode>();
                var itarget = n.Values["target"].As<INode>();
                var source = ParseNode(isource);
                var target = ParseNode(itarget);

                rndNodes[isource.Id] = source;
                rndNodes[itarget.Id] = target;
                rndEdges.Add(n.Values["edge"].As<IRelationship>());
            }

            return (rndNodes, rndEdges);
        }

        private async Task<(Dictionary<long, Node>, List<IRelationship>)> GetNeighbors(
            string rootScriptAddress, int maxHops)
        {
            using var session = _driver.AsyncSession(
                x => x.WithDefaultAccessMode(AccessMode.Read));

            var samplingResult = session.ReadTransactionAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"MATCH path = (p: Script {{ Address: \"{rootScriptAddress}\"}}) -[:Transfer * 1..{maxHops}]->(p2: Script) " +
                    "WITH p, [n in nodes(path) where n <> p | n] as nodes, relationships(path) as relationships " +
                    "WITH collect(distinct p) as root, size(nodes) as cnt, collect(nodes[-1]) as nodes, collect(distinct relationships[-1]) as relationships " +
                    "RETURN root, nodes, relationships");

                /* Note:
                 * Neo4j has apoc.neighbors.byhop method that returns 
                 * neighbors at n-hop distance. However, this method 
                 * does not return relationships, therefore, the above
                 * cypher query is used instead. 
                 */

                /* TODO:
                 * Modify the above cypher query to return only one root, 
                 * it currently returns one root per hop where root nodes
                 * of all the hops are equal.
                 */
                return await result.ToListAsync();
            });
            await samplingResult;

            var nodes = new Dictionary<long, Node>();
            var edges = new List<IRelationship>();

            foreach (var hop in samplingResult.Result)
            {
                var root = hop.Values["root"].As<List<INode>>()[0];
                nodes[root.Id] = ParseNode(root);

                var neo4jNodes = hop.Values["nodes"].As<List<object>>();
                var neo4jEdges = hop.Values["relationships"].As<List<object>>();

                foreach (var neo4jNode in neo4jNodes)
                {
                    var node = neo4jNode.As<INode>();
                    nodes[node.Id] = ParseNode(node);
                }

                foreach (var neo4jEdge in neo4jEdges)
                    edges.Add(neo4jEdge.As<IRelationship>());
            }

            return (nodes, edges);
        }

        private static (List<double[]>, List<double[]>, List<int[]>) ToMatrix(Dictionary<long, Node> nodes, List<IRelationship> edges)
        {
            var nodeFeatures = new List<double[]>();
            var nodeIdToIdx = new Dictionary<long, int>();
            foreach (var node in nodes)
            {
                nodeFeatures.Add(node.Value.GetFeatures());
                nodeIdToIdx.Add(node.Key, nodeIdToIdx.Count);
            }

            var edgeFeatures = new SortedList<int[], double[]>(
                Comparer<int[]>.Create((x, y) =>
                {
                    // This comparer allows duplicates,
                    // and treats equal items as x greater than y.
                    var r = x[0].CompareTo(y[0]);
                    if (r != 0) return r;
                    r = x[1].CompareTo(y[1]);
                    if (r != 0) return r;
                    return 1;
                }));


            foreach (var edge in edges)
            {
                var e = new Edge(
                    nodes[edge.StartNodeId],
                    nodes[edge.EndNodeId],
                    (double)edge.Properties["Value"],
                    Enum.Parse<EdgeType>(edge.Type),
                    0, // TODO: fixme.
                    (int)(long)edge.Properties["Height"]); // TODO: fixme: here we are casting from int64 to int32, how hard is it to change the type of Height in the Edge from 32-bit int to 64-bit int?

                edgeFeatures.Add(
                    new int[] { nodeIdToIdx[edge.StartNodeId], nodeIdToIdx[edge.EndNodeId] },
                    e.GetFeatures());

            }

            var pairIndices = edgeFeatures.Keys.ToList();
            return (nodeFeatures, edgeFeatures.Values.ToList(), pairIndices);
        }

        private static Node ParseNode(INode node)
        {
            var props = node.Properties;

            return new Node(
                node.Id.ToString(),
                (string)props["Address"],
                Enum.Parse<ScriptType>((string)props["ScriptType"]));
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

