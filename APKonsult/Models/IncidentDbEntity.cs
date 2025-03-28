﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace APKonsult.Models;

public class IncidentDbEntity
{
    [Column("id"), Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public ulong Id { get; set; }

    [Column("guild_id")]
    public ulong GuildId { get; set; }

    [Column("target_id")]
    public ulong TargetId { get; set; }

    [Column("moderator_id")]
    public ulong ModeratorId { get; set; }

    [Column("reason")]
    public string Reason { get; set; } = "not given";

    public GuildDbEntity Guild { get; set; }
    public UserDbEntity TargetUser { get; set; }
    public DateTimeOffset CreationTimeStamp { get; set; }
}