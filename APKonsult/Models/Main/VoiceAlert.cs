using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models.Main;

public class VoiceAlert
{
    [Column("id"), Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong AlertId { get; set; }

    [Column("channel_id")]
    public ulong ChannelId { get; set; }

    [Column("guild_id")]
    public ulong GuildId { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("is_repeatable")]
    public bool IsRepeatable { get; set; }

    [Column("last_alert")]
    public DateTimeOffset? LastAlert { get; set; }

    [Column("time_between")]
    public TimeSpan? MinTimeBetweenAlerts { get; set; }

    public UserDbEntity User { get; set; }
}