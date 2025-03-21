using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models;

public class UserDbEntity
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    public DbSet<IncidentDbEntity> Incidents { get; set; }
    public DbSet<ReminderDbEntity> Reminders { get; set; }
    public DbSet<VoiceAlert> VoiceAlerts { get; set; }
    public DbSet<MessageTag> MessageAliases { get; set; }

    public AfkStatusEntity? AfkStatus { get; set; }

    /// <summary>
    /// Can use special commands
    /// </summary>
    [DefaultValue(false)]
    [Column("is_bot_admin")]
    public bool IsBotAdmin { get; set; }

    /// <summary>
    /// Used with <see cref="FinderBot.Commands.UserReactionCommand"/>
    /// </summary>
    [Column("reaction_emoji")]
    [DefaultValue("")]
    public string? ReactionEmoji { get; set; }
}

public class UserDbEntityConfig : IEntityTypeConfiguration<UserDbEntity>
{
    public void Configure(EntityTypeBuilder<UserDbEntity> builder)
    {
        builder.ToTable("users");

        builder.HasKey(x => x.Id);

        builder.HasMany(x => x.Incidents)
            .WithOne(x => x.TargetUser)
            .HasForeignKey(x => x.TargetId);

        builder.HasMany(x => x.Reminders)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.VoiceAlerts)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.MessageAliases)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.AfkStatus);
    }
}