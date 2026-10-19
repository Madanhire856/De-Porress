using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SMS.Data;

namespace SMS.Web.Services;

// Migration scaffolding does not need Cognito configuration or a live database.
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SMSDbContext>
{
    public SMSDbContext CreateDbContext(string[] args) => new(new DbContextOptionsBuilder<SMSDbContext>()
        .UseSqlServer(Environment.GetEnvironmentVariable("ConnectionStrings__SMSDb")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=DePorresDesign;Integrated Security=True;Encrypt=True")
        .Options);
}
