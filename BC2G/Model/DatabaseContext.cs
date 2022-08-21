using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BC2G.Model
{
    internal class DatabaseContext : DbContext
    {
        private readonly string _host;
        private readonly string _database;
        private readonly string _username;
        private readonly string _password;

        public DbSet<Utxo> Utxos { set; get; }

        // TODO: Currently this constructor is needed to add migration scripts, 
        // see how this requirement can be better addressed. 
        public DatabaseContext() { }

        public DatabaseContext(string host, string database, string username, string password)
        {
            _host = host;
            _database = database;
            _username = username;
            _password = password;
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Utxo>().HasKey(x => x.Id);
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(
                $"Host={_host};" +
                $"Database={_database};" +
                $"Username={_username};" +
                $"Password={_password}");
        }
    }
}
