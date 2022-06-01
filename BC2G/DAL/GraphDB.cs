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

        ~GraphDB() => Dispose(false);

        public GraphDB(string uri, string user, string password)
        {
            _driver = GraphDatabase.Driver(uri, AuthTokens.Basic(user, password));

            try { _driver.VerifyConnectivityAsync().Wait(); }
            catch (AggregateException) { Dispose(true); throw; }
            // TODO: it seems message of an exception cannot be modified without impacting 
            // stacktrace. Check if there is a better way of throwing an error with a message
            // that indicates not able to connect to the Neo4j database.

            EnsureCoinbaseNode().Wait();
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
                            await tx.RunAsync($"CREATE (:{Coinbase})");
                        });
                    }
                    break;
                default:
                    // TODO: replace with a more suitable exception type. 
                    throw new Exception($"Found {count} {Coinbase} nodes; expected zero or one.");
            }
        }

        public async Task AddBlock(Block block)
        {
            using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write));
            await session.WriteTransactionAsync(async tx =>
            {
                var result = await tx.RunAsync(
                    $"MERGE (x:{block.Height}:{block.MedianTime} " +
                    $"{{difficulty: {block.Difficulty}, " +
                    $"confirmations: {block.Confirmations}, " +
                    $"tx_count: {block.TransactionsCount}, " +
                    $"stripped_size: {block.StrippedSize}, " +
                    $"size: {block.Size}, " +
                    $"weight: {block.Weight}}}");
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
                    $"MATCH (b:_{block.Height}:_{block.MedianTime}) " +
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

        public static void Run()
        {
            using (var greeter = new GraphDB("bolt://localhost:7687", "neo4j", "password"))
            {
                greeter.PrintGreeting("hello, world");
            }
        }
    }
}
