using Microsoft.EntityFrameworkCore;
using FitbitDashboard.Shared;

namespace FitbitDashboard.API;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<UserFitbitToken> FitbitTokens => Set<UserFitbitToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Define our fixed string ID as the primary key
        modelBuilder.Entity<UserFitbitToken>().HasKey(t => t.Id);
    }
}
