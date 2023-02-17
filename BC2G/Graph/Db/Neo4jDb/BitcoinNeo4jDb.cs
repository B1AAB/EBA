using BC2G.Graph.Db.Neo4jDb.BitcoinMappers;

namespace BC2G.Graph.Db.Neo4jDb;

public class BitcoinNeo4jDb : Neo4jDb<BlockGraph>
{
    public static string Coinbase { get { return "Coinbase"; } }

    public BitcoinNeo4jDb(Options options, ILogger<BitcoinNeo4jDb> logger) :
        base(options, logger, new MapperFactory())
    {
        if (!Options.Bitcoin.SkipGraphLoad)
        {
            EnsureCoinbaseNodeAsync().Wait();
            EnsureConstraintsAsync().Wait();
        }
    }


    private async Task EnsureCoinbaseNodeAsync()
    {
        int count = 0;
        using (var session = Driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Read)))
        {
            count = await session.ExecuteReadAsync(async tx =>
            {
                var result = await tx.RunAsync($"MATCH (n:{BitcoinAgent.Coinbase}) RETURN COUNT(n)");
                return result.SingleAsync().Result[0].As<int>();
            });
        }

        switch (count)
        {
            case 1: return;
            case 0:
                using (var session = Driver.AsyncSession(x => x.WithDefaultAccessMode(AccessMode.Write)))
                {
                    await session.ExecuteWriteAsync(async tx =>
                    {
                        await tx.RunAsync(
                            $"CREATE (:{BitcoinAgent.Coinbase} {{" +
                            $"{Props.ScriptAddress.Name}: " +
                            $"\"{BitcoinAgent.Coinbase}\"}})");
                    });
                }
                break;
            default:
                // TODO: replace with a more suitable exception type. 
                throw new Exception($"Found {count} {Coinbase} nodes; expected zero or one.");
        }
    }

    private async Task EnsureConstraintsAsync()
    {
        using var session = Driver.AsyncSession(
            x => x.WithDefaultAccessMode(AccessMode.Write));

        // TODO: do not create contraints if they already exist,
        // otherwise you'll get the following error: 
        //
        // One or more errors occurred. (An equivalent constraint
        // already exists, 'Constraint( id=4,
        // name='UniqueAddressContraint', type='UNIQUENESS',
        // schema=(:Script {Address}), ownedIndex=3 )'.)
        //
        // Solution: first check if the constraint exists, and only 
        // create if not.
        //
        // Check if existing contraints as the following, and add contrains only
        // if they are not already defined. Alternatively, try creating the contrains, 
        // and if they already exist, you'll see an Exception (non-blocking) in the
        // above code. 
        /*var xyz = await session.ReadTransactionAsync(async x =>
        {
            var result = await x.RunAsync("CALL db.constraints");
            return result.ToListAsync();
        });*/

        // TODO: handle the exceptions raised in running the following.
        // Note that the exceptions are stored in the Exceptions property
        // and do not log and stop execution when raised. 

        try
        {
            await session.ExecuteWriteAsync(async x =>
            {
                var result = await x.RunAsync(
                    "CREATE CONSTRAINT UniqueAddressContraint " +
                    $"FOR (script:{ScriptMapper.labels}) " +
                    $"REQUIRE script.{Props.ScriptAddress.Name} IS UNIQUE");
            });
        }
        catch (Exception e)
        {

        }

        try
        {
            await session.ExecuteWriteAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"CREATE INDEX FOR (script:{ScriptMapper.labels}) " +
                    $"ON (script.{Props.ScriptAddress.Name})");
            });
        }
        catch (Exception e)
        {

        }

        try
        {
            await session.ExecuteWriteAsync(async x =>
            {
                var result = await x.RunAsync(
                    $"CREATE INDEX FOR (block:{BlockMapper.label})" +
                    $" on (block.{Props.Height.Name})");
            });
        }
        catch (Exception e)
        {

        }
    }
}
