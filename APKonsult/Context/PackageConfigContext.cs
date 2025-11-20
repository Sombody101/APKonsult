using APKonsult.Models.PackageConfigs;
using Microsoft.EntityFrameworkCore;

namespace APKonsult.Context;

public class PackageConfigContext : DbContext
{
    public PackageConfigContext(DbContextOptions<PackageConfigContext> options)
        : base(options)
    { }

    public DbSet<Package> Packages;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(PackageConfigContext).Assembly);

        _ = modelBuilder.Entity<Package>()
            .OwnsOne(a => a.Configurations, ownedBuilder =>
            {
                ownedBuilder.ToJson();
            });

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
#endif
    }
}
