namespace BC2G.Infrastructure;

public class DatabaseContext : DbContext
{
    public DbSet<Utxo> Utxos => Set<Utxo>();

    public DatabaseContext(
        DbContextOptions<DatabaseContext> options) :
        base(options)
    { }


    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<Utxo>().HasKey(x => x.Id);
    }

    public static async Task OptimisticAddOrUpdateAsync(
        ICollection<Utxo> utxos,
        IDbContextFactory<DatabaseContext> contextFactory,
        ILogger<DatabaseContext> logger,
        CancellationToken ct)
    {
        await OptimisticTxAsync(
            async () =>
            {
                ct.ThrowIfCancellationRequested();

                logger.LogInformation(
                    "Trying to optimistically add UTXOs to the database. " +
                    "UTXOs count: {c:n0}", utxos.Count);

                using var c = contextFactory.CreateDbContext();
                await c.Utxos.AddRangeAsync(utxos, ct);
                await c.SaveChangesAsync(ct);

                logger.LogInformation("Finished optimistically adding UTXO's to the database.");
            },
            async () =>
            {
                ct.ThrowIfCancellationRequested();

                logger.LogInformation(
                    "Failed optimistically adding UTXOs. " +
                    "This could happen if the database already contained the UTXOs. " +
                    "Are you repeating a traverse? " +
                    "Trying to resiliently add UTXOs to the database; " +
                    "this could take a while.");

                await ResilientAddOrUpdateAsync(utxos, contextFactory, ct);

                logger.LogInformation("Finished resiliently adding UTXOs to the database.");
            });
    }

    public static void OptimisticAddOrUpdate(
        object dbLock,
        IEnumerable<Utxo> utxos,
        IDbContextFactory<DatabaseContext> contextFactory,
        ILogger<DatabaseContext> logger)
    {
        lock (dbLock)
        {
            OptimisticTx(
                () =>
                {
                    logger.LogInformation(
                        "Trying to optimistically add UTXOs to the database. " +
                        "UTXOs count: {c:n0}", utxos.Count());

                    using (var c = contextFactory.CreateDbContext())
                    {
                        logger.LogInformation("Created database context, adding the utxos.");
                        c.Utxos.AddRange(utxos);
                        logger.LogInformation("Utxos are added, saving changes to the database context.");
                        c.SaveChanges();
                        logger.LogInformation("Changes to the database context are saved.");
                    }

                    logger.LogInformation("Finished optimistically adding UTXO's to the database.");
                },
                () =>
                {
                    logger.LogWarning(
                        "Failed optimistically adding UTXOs. " +
                        "This could happen if the database already contained the UTXOs. " +
                        "Are you repeating a traverse? " +
                        "Trying to resiliently add UTXOs to the database; " +
                        "this could take a while.");

                    ResilientAddOrUpdate(utxos, contextFactory);

                    logger.LogInformation("Finished resiliently adding UTXOs to the database.");
                });
        }
    }

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
                    inDbUtxo.AddCreatedIn(utxo.CreatedIn, utxo.CreatedInHeight);
                    inDbUtxo.AddReferencedIn(utxo.ReferencedIn, utxo.ReferencedInHeight);
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
                ct.ThrowIfCancellationRequested();
                using var c = contextFactory.CreateDbContext();

                var inDbUtxo = await c.Utxos.FindAsync(utxo.Id, ct);
                if (inDbUtxo == null)
                {
                    await c.Utxos.AddAsync(utxo, ct);
                }
                else
                {
                    inDbUtxo.AddCreatedIn(utxo.CreatedIn, utxo.CreatedInHeight);
                    inDbUtxo.AddReferencedIn(utxo.ReferencedIn, utxo.ReferencedInHeight);
                }

                await c.SaveChangesAsync(ct);
            });
        }
    }
}
