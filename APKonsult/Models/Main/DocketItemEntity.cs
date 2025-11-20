using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace APKonsult.Models.Main;

public class DocketItemEntity
{
    [Column("id"), Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [Column("item_name")]
    public string Name { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("status")]
    public DocketItemStatus Status { get; set; } = DocketItemStatus.Open;

    [Column("close_reason")]
    public string CloseReason { get; set; } = string.Empty;
}

public enum DocketItemStatus
{
    Open,
    Closed,
    Ignored,
}