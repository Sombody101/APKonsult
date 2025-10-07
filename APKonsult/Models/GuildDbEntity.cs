using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models;

public class GuildDbEntity
{
    public GuildDbEntity(ulong id)
    {
        Id = id;
        Settings = new GuildConfigDbEntity();
    }

    public GuildDbEntity(DiscordGuild guild)
        : this(guild.Id)
    {
        Name = guild.Name;
    }

    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; init; }

    [Column("name")]
    [DefaultValue("")]
    public string Name { get; set; }

    public GuildConfigDbEntity Settings { get; set; }

    public List<IncidentDbEntity> Incidents { get; set; } = [];

    public List<QuoteDbEntity> Quotes { get; set; } = [];

    public List<TrackingDbEntity> TrackingConfigurations { get; set; } = [];

    public List<EventAction> DefinedActions { get; set; } = [];

    public List<DocketItemEntity> Docket { get; set; } = [];
}

public class GuildDbEntityConfig : IEntityTypeConfiguration<GuildDbEntity>
{
    public void Configure(EntityTypeBuilder<GuildDbEntity> builder)
    {
        builder.ToTable("guilds");

        builder.HasKey(x => x.Id);

        builder.HasOne(x => x.Settings)
            .WithOne(x => x.Guild)
            .HasForeignKey<GuildConfigDbEntity>(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Incidents)
            .WithOne(x => x.Guild)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Quotes)
            .WithOne(x => x.Guild)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.DefinedActions)
            .WithOne(x => x.Guild)
            .HasForeignKey(x => x.GuildId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}