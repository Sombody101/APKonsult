using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models;

public class BlacklistedDbEntity
{
    [Column("id"), Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; } = 0;

    [Column("guild_id")]
    public ulong GuildId { get; set; } = 0;

    [Column("ban_reason")]
    public string Reason { get; set; } = string.Empty;

    public ulong GetId()
    {
        // If the user is 0, then the guild was blacklisted
        if (UserId is 0)
            return GuildId;

        return UserId;
    }

    public string BanReason()
    {
        if (string.IsNullOrWhiteSpace(Reason))
            return "No reason supplied";

        return Reason;
    }
}