using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Pharmaceutical.Infrastructure;

public class PharmaceuticalDbContextFactory : IDesignTimeDbContextFactory<PharmaceuticalDbContext>
{
    public PharmaceuticalDbContext CreateDbContext(string[] args)
    {
        var webApiDir = FindWebApiDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(webApiDir)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' not found. Configure Pharmaceutical.WebAPI/appsettings.Development.json or set ConnectionStrings__DefaultConnection.");

        var optionsBuilder = new DbContextOptionsBuilder<PharmaceuticalDbContext>();
        optionsBuilder.UseMySql(
            connectionString,
            new MySqlServerVersion(new Version(8, 0, 36)));

        return new PharmaceuticalDbContext(optionsBuilder.Options);
    }

    private static string FindWebApiDirectory()
    {
        var dir = Directory.GetCurrentDirectory();
        while (dir != null)
        {
            var candidate = Path.Combine(dir, "Pharmaceutical.WebAPI");
            if (File.Exists(Path.Combine(candidate, "appsettings.json")))
                return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }

        throw new InvalidOperationException("Could not locate Pharmaceutical.WebAPI directory.");
    }
}
