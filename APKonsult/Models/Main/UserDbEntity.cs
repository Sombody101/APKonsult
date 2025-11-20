using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models.Main;

public class UserDbEntity
{
    public UserDbEntity()
    {
    }

    public UserDbEntity(DiscordUser transport)
    {
        Id = transport.Id;
        Username = transport.Username;
        Discriminator = transport.Discriminator;
        GlobalName = transport.GlobalName;
        AvatarHash = transport.AvatarHash;
        BannerHash = transport.BannerHash;
        IsBot = transport.IsBot;
        Flags = transport.Flags;
        OAuthFlags = transport.OAuthFlags;
    }

    public UserDbEntity(DiscordUser transport, DateTimeOffset joinDate)
        : this(transport)
    {
        JoinDate = joinDate;
    }

    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public ulong Id { get; set; }

    [Column("username")]
    public string Username { get; set; } = string.Empty;

    [Column("discriminator")]
    public string? Discriminator { get; set; } = string.Empty;

    [Column("global_name")]
    public string? GlobalName { get; set; } = string.Empty;

    [Column("avatar_hash")]
    public string? AvatarHash { get; set; } = string.Empty;

    [Column("banner_hash")]
    public string? BannerHash { get; set; } = string.Empty;

    [Column("is_bot")]
    public bool IsBot { get; set; } = false;

    public DiscordUserFlags? Flags { get; set; }

    public DiscordUserFlags? OAuthFlags { get; set; }

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

    [Column("join_date")]
    public DateTimeOffset JoinDate { get; set; }

    public void UpdateUser(DiscordUser transport)
    {
        if (transport.Id != Id)
        {
            throw new InvalidOperationException("The passed user info does not have the same user ID as the current user.");
        }

        Username = transport.Username;
        Discriminator = transport.Discriminator;
        GlobalName = transport.GlobalName;
        AvatarHash = transport.AvatarHash;
        BannerHash = transport.BannerHash;
        IsBot = transport.IsBot;
        Flags = transport.Flags;
        OAuthFlags = transport.OAuthFlags;
    }
}

public class UserDbEntityConfig : IEntityTypeConfiguration<UserDbEntity>
{
    public void Configure(EntityTypeBuilder<UserDbEntity> builder)
    {
        _ = builder.ToTable("users");

        _ = builder.HasKey(x => x.Id);

        _ = builder.HasMany(x => x.Incidents)
            .WithOne(x => x.TargetUser)
            .HasForeignKey(x => x.TargetId);

        _ = builder.HasMany(x => x.Reminders)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasMany(x => x.VoiceAlerts)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasMany(x => x.MessageAliases)
            .WithOne(x => x.User)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        _ = builder.HasOne(x => x.AfkStatus);
    }
}