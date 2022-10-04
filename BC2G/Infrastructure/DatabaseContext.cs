using BC2G.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;
using Polly;
using Polly.Contrib.WaitAndRetry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.Infrastructure
{
    /*
        public class BloggingContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
        {
            public DatabaseContext CreateDbContext(string[] args)
            {
                Console.WriteLine("I am used.------------------");
                var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();
                optionsBuilder.UseNpgsql("Data Source=blog.db");

                return new DatabaseContext(optionsBuilder.Options);
            }
        }*/

    /*
    public class DatabaseContext : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        private readonly string _connectionString;

        public DatabaseContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            Console.WriteLine("I am called ***********************");
            optionsBuilder.UseNpgsql("Host=localhost");
        }
    }*/


    public class DatabaseContext : DbContext
    {
        /*
        private readonly string _host;
        private readonly string _database;
        private readonly string _username;
        private readonly string _password;*/

        public DbSet<Utxo> Utxos => Set<Utxo>();
        //public DbSet<UtxoB> UtxosB;

        // TODO: Currently this constructor is needed to add migration scripts, 
        // see how this requirement can be better addressed. 
        /*public DatabaseContext() : base()
        {
            Console.WriteLine("I am called 2");
        }*/

        // Needed by the migration scripts
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        { }

        /*
        public DatabaseContext(string host, string database, string username, string password)
        {
            _host = host;
            _database = database;
            _username = username;
            _password = password;
        }*/

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Utxo>().HasKey(x => x.Id);

            // This is used to set a system column as
            // concurrency token for optimistic concurrency
            // since Psql does not currently have
            // auto-incremented columns.
            // Read the following docs for details:
            // https://www.npgsql.org/efcore/modeling/concurrency.html
            builder.Entity<Utxo>().UseXminAsConcurrencyToken();
        }

        public static async Task OptimisticAddOrUpdate(
            DatabaseContext context,
            IDbContextFactory<DatabaseContext> contextFactory,
            CancellationToken ct)
        {
            await OptimisticTx(async () =>
            {
                await context.SaveChangesAsync(ct);
            },
            async () =>
            {
                var enties = context.ChangeTracker.Entries<Utxo>();
                var utxos = enties.Select(x => (Utxo)x.CurrentValues.ToObject());
                await ResilientAddOrUpdate(utxos, contextFactory, ct);
            });
        }

        public static async Task OptimisticAddOrUpdate(
            ICollection<Utxo> utxos,
            IDbContextFactory<DatabaseContext> contextFactory,
            CancellationToken ct)
        {
            await OptimisticTx(
                async () =>
                {
                    using var c = contextFactory.CreateDbContext();
                    await c.Utxos.AddRangeAsync(utxos, ct);
                    await c.SaveChangesAsync(ct);
                },
                async () =>
                {
                    await ResilientAddOrUpdate(utxos, contextFactory, ct);
                });
        }

        private static async Task OptimisticTx(Func<Task> tx, Func<Task> onFailure)
        {
            try { await tx(); }
            catch (Exception e)
            {
                switch (e)
                {
                    case InvalidOperationException:
                    case DbUpdateConcurrencyException:
                    case DbUpdateException when (
                        e.InnerException is PostgresException pe &&
                        pe.SqlState == "23505"):
                        await onFailure();
                        break;

                    default: throw;
                }
            }
        }

        private static async Task ResilientAddOrUpdate(
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
}





