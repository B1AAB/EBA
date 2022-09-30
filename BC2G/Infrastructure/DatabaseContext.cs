using BC2G.Model;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.Infrastructure
{
    public class DatabaseContext : DbContext
    {
        /*
        private readonly string _host;
        private readonly string _database;
        private readonly string _username;
        private readonly string _password;*/

        public DbSet<Utxo> Utxos => Set<Utxo>();

        // TODO: Currently this constructor is needed to add migration scripts, 
        // see how this requirement can be better addressed. 
        public DatabaseContext() { }
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options) { }

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
        /*
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(
                $"Host={_host};" +
                $"Database={_database};" +
                $"Username={_username};" +
                $"Password={_password}");
        }*/
    }
}
