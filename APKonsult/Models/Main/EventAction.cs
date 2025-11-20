using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models.Main;

public class EventAction
{
    [Key]
    public int Id { get; set; }

    [Column("event_name")]
    public required string EventName { get; set; }

    [Column("guild_id")]
    public ulong GuildId { get; set; }

    [Column("lua_script")]
    public required string LuaScript { get; set; }

    [Column("action_name")]
    public required string ActionName { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; } = false;

    public GuildDbEntity Guild { get; set; }

    public override string ToString()
    {
        return $"[act:{ActionName}, event:{EventName}]";
    }
}
