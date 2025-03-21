using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models;

public class TrackingDbEntity
{
    [Column("id"), Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong ConfigId { get; set; }

    /// <summary>
    /// The name given to this config
    /// </summary>
    [Column("name")]
    public string Name { get; set; }

    [Required]
    public ulong GuildId { get; set; }

    /// <summary>
    /// When this config was created
    /// </summary>
    [Column("creation_epoch")]
    public long CreationEpoch { get; set; }

    /// <summary>
    /// The channel for the bot to search in
    /// </summary>
    [Column("channel_id")]
    public ulong SourceChannelId { get; set; }

    /// <summary>
    /// The channel for the bot to report in when a message is flagged
    /// </summary>
    [Column("report_channel")]
    public ulong ReportChannelId { get; set; }

    /// <summary>
    /// The regex used to flag a message
    /// </summary>
    [Column("tracking_regex")]
    public string RegexPattern { get; set; }

    /// <summary>
    /// A string of IDs for all users who have edited this config
    /// </summary>
    [Column("editor_list")]
    public string EditorList { get; set; }

    [Column("items_flagged")]
    public uint BeenFlagged { get; set; }

    public void IncrementUse() => ++BeenFlagged;
}

public class TagEntityConfiguration
    : IEntityTypeConfiguration<TrackingDbEntity>
{
    public void Configure(
        EntityTypeBuilder<TrackingDbEntity> entityTypeBuilder)
    {
        entityTypeBuilder
            .Property(x => x.GuildId)
            .HasConversion<long>();

        entityTypeBuilder
            .HasIndex(x => x.GuildId);

        entityTypeBuilder
            .HasIndex(x => x.Name);
    }
}