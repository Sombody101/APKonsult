using APKonsult.Models;
using Microsoft.EntityFrameworkCore;

namespace APKonsult.Context;

public class APKonsultContext : DbContext
{
    public APKonsultContext(DbContextOptions<APKonsultContext> options)
        : base(options)
    { }

    public DbSet<BlacklistedDbEntity> Blacklist { get; set; }

    public DbSet<UserDbEntity> Users { get; set; }
    public DbSet<GuildDbEntity> Guilds { get; set; }
    public DbSet<GuildConfigDbEntity> Configs { get; set; }
    public DbSet<IncidentDbEntity> Incidents { get; set; }
    public DbSet<StarboardMessageDbEntity> Starboard { get; set; }
    public DbSet<QuoteDbEntity> Quotes { get; set; }
    public DbSet<ReminderDbEntity> Reminders { get; set; }
    public DbSet<VoiceAlert> VoiceAlerts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        _ = modelBuilder.ApplyConfigurationsFromAssembly(typeof(APKonsultContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        _ = optionsBuilder.UseSqlite(APKonsultBot.DB_CONNECTION_STRING);

#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
#endif
    }
}