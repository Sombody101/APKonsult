using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models;

public class GuildConfigDbEntity
{
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; init; }

    /// <summary>
    /// Snowflake id of the guild the config is related to
    /// </summary>
    [Required, Column("discordId")]
    public ulong GuildId { get; set; }

    [Column("prefix"), MaxLength(5)]
    public string Prefix { get; set; } = string.Empty;

    [Column("starboard_enabled")]
    public bool StarboardActive { get; set; }

    [Column("starboard_channel")]
    public ulong? StarboardChannelId { get; set; }

    [Column("starboard_threshold")]
    public int? StarboardThreshold { get; set; }

    [Column("starboard_emoji_id")]
    public ulong? StarboardEmojiId { get; set; }

    [Column("starboard_emoji_name"), MaxLength(50)]
    public string? StarboardEmojiName { get; set; }

    public GuildDbEntity Guild;
}