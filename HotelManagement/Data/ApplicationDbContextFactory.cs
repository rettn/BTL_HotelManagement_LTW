using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace HotelManagement.Data;

/// <summary>
/// Lets EF tooling inspect the model and generate migrations without connecting to MySQL.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection.");

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new ApplicationDbContext(options);
    }
}
