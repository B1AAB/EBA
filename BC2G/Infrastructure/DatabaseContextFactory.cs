using Microsoft.EntityFrameworkCore.Design;

namespace BC2G.Infrastructure;

// This is used for creating migration scripts. 

public class DatabaseContextFactory : IDesignTimeDbContextFactory<DatabaseContext>
{
    public DatabaseContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DatabaseContext>();

        var psqlDefaults = new PsqlOptions();
        optionsBuilder.UseNpgsql(
                $"Host={psqlDefaults.Host};" +
                $"Database={psqlDefaults.Database};" +
                $"Username={psqlDefaults.Username};" +
                $"Password={psqlDefaults.Password}");

        return new DatabaseContext(optionsBuilder.Options);
    }
}
