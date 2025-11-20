using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models.Main;

public class AfkStatusEntity
{
    [Column("id"), Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [Column("afk_message"), MaxLength(70)]
    public string? AfkMessage { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; }

    [Column("afk_epoch")]
    public long AfkEpoch { get; set; }

    public required UserDbEntity User { get; set; }
}

public static class Ext
{
    public static bool IsAfk(this AfkStatusEntity? status)
    {
        return status is not null && !string.IsNullOrWhiteSpace(status.AfkMessage);
    }
}