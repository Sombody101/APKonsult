using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models;

public class MessageTag
{
    [Column("id"), Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [Column("tag_name")]
    public string Name { get; set; }

    [Column("tag_data"), MaxLength(1024 * 2)] // 2KB max size
    public string Data { get; set; }

    [Column("user_id")]
    public ulong UserId { get; set; }

    public UserDbEntity User { get; set; }
}