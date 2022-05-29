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

            EnsureCoinbaseNode().Wait();
            
        }

        private async Task EnsureCoinbaseNode()
        {
            using var session = _driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read));
            var count = await session.ReadTransactionAsync(async tx =>
            {
                var result = await tx.RunAsync($"MATCH (n:{Coinbase}) RETURN COUNT(n)");
                return result.SingleAsync().Result[0].As<int>();
            });

            switch (count)
            {
                case 1: return;
                case 0:
                    await session.WriteTransactionAsync(async tx =>
                    {
                        await tx.RunAsync($"CREATE (:{Coinbase})");
                    });
                    break;
                default:
                    // TODO: replace with a more suitable exception type. 
                    throw new Exception($"Found {count} {Coinbase} nodes; expected zero or one.");
            }
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
