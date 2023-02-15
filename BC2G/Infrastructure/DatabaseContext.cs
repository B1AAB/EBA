namespace BC2G.Infrastructure;

public class DatabaseContext : DbContext
{
    public DbSet<Utxo> Utxos => Set<Utxo>();

    // Needed by the migration scripts
    public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
    { }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Utxo>().HasKey(x => x.Id);
    }

    public static async Task OptimisticAddOrUpdateAsync(
        DatabaseContext context,
        IDbContextFactory<DatabaseContext> contextFactory,
        CancellationToken ct)
    {
        await OptimisticTxAsync(
            async () =>
            {
                await context.SaveChangesAsync(ct);
            },
            async () =>
            {
                var enties = context.ChangeTracker.Entries<Utxo>();
                var utxos = enties.Select(x => (Utxo)x.CurrentValues.ToObject());
                await ResilientAddOrUpdateAsync(utxos, contextFactory, ct);
            });
    }

    public static void OptimisticAddOrUpdate(
        object dbLock,
        ICollection<Utxo> utxos,
        IDbContextFactory<DatabaseContext> contextFactory)
    {
        lock (dbLock)
        {
            OptimisticTx(
                () =>
                {
                    using var c = contextFactory.CreateDbContext();
                    c.Utxos.AddRange(utxos);
                    c.SaveChanges();
                },
                () =>
                {
                    ResilientAddOrUpdate(utxos, contextFactory);
                });
        }
    }

    /*
    public static async Task OptimisticAddOrUpdateAsync(
        ICollection<Utxo> utxos,
        IDbContextFactory<DatabaseContext> contextFactory,
        CancellationToken ct)
    {
        await OptimisticTxAsync(
            async () =>
            {
                using var c = contextFactory.CreateDbContext();
                await c.Utxos.AddRangeAsync(utxos, ct);
                await c.SaveChangesAsync(ct);
            },
            async () =>
            {
                await ResilientAddOrUpdateAsync(utxos, contextFactory, ct);
            });
    }*/

    private static async Task OptimisticTxAsync(Func<Task> txAsync, Func<Task> onFailureAsync)
    {
        try { await txAsync(); }
        catch (Exception e)
        {
            switch (e)
            {
                case InvalidOperationException:
                case DbUpdateConcurrencyException:
                case DbUpdateException when (
                    e.InnerException is PostgresException pe &&
                    pe.SqlState == "23505"):
                    await onFailureAsync();
                    break;

                default: throw;
            }
        }
    }

    private static void OptimisticTx(Action tx, Action onFailure)
    {
        try { tx(); }
        catch (Exception e)
        {
            switch (e)
            {
                case InvalidOperationException:
                case DbUpdateConcurrencyException:
                case DbUpdateException when (
                    e.InnerException is PostgresException pe &&
                    pe.SqlState == "23505"):
                    onFailure();
                    break;

                default: throw;
            }
        }
    }

    private static void ResilientAddOrUpdate(
        IEnumerable<Utxo> utxos, IDbContextFactory<DatabaseContext> contextFactory)
    {
        foreach (var utxo in utxos)
        {
            var policy = Policy
                .Handle<InvalidOperationException>()
                .Or<DbUpdateConcurrencyException>()
                .Or<DbUpdateException>(
                    e => e.InnerException is PostgresException pe &&
                    pe.SqlState == "23505")
                .WaitAndRetry(Backoff.DecorrelatedJitterBackoffV2(
                    TimeSpan.FromSeconds(10), 3));

            policy.Execute(() =>
            {
                using var c = contextFactory.CreateDbContext();
                var inDbUtxo = c.Utxos.Find(utxo.Id);
                if (inDbUtxo == null)
                {
                    c.Utxos.Add(utxo);
                }
                else
                {
                    inDbUtxo.AddCreatedIn(utxo.CreatedIn);
                    inDbUtxo.AddReferencedIn(utxo.ReferencedIn);
                }

                c.SaveChanges();
            });
        }
    }

    private static async Task ResilientAddOrUpdateAsync(
        IEnumerable<Utxo> utxos,
        IDbContextFactory<DatabaseContext> contextFactory,
        CancellationToken ct)
    {
        foreach (var utxo in utxos)
        {
            var policy = Policy
                .Handle<InvalidOperationException>()
                .Or<DbUpdateConcurrencyException>()
                .Or<DbUpdateException>(
                    e => e.InnerException is PostgresException pe &&
                    pe.SqlState == "23505")
                .WaitAndRetryAsync(Backoff.DecorrelatedJitterBackoffV2(
                    TimeSpan.FromSeconds(10), 3));

            await policy.ExecuteAsync(async () =>
            {
                using var c = contextFactory.CreateDbContext();
                var inDbUtxo = await c.Utxos.FindAsync(utxo.Id);
                if (inDbUtxo == null)
                {
                    await c.Utxos.AddAsync(utxo, ct);
                }
                else
                {
                    inDbUtxo.AddCreatedIn(utxo.CreatedIn);
                    inDbUtxo.AddReferencedIn(utxo.ReferencedIn);
                }

                await c.SaveChangesAsync(ct);
            });
        }
    }
}
