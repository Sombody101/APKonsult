using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APKonsult.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Blacklist",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    user_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    guild_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ban_reason = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Blacklist", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "guilds",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_guilds", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Starboard",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    discordMessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    discordChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    discordGuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    starCount = table.Column<int>(type: "INTEGER", nullable: false),
                    starboardMessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    starboardChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    starboardGuildId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Starboard", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    username = table.Column<string>(type: "TEXT", nullable: false),
                    is_bot_admin = table.Column<bool>(type: "INTEGER", nullable: false),
                    reaction_emoji = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Configs",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    discordId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    prefix = table.Column<string>(type: "TEXT", maxLength: 5, nullable: false),
                    starboardEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    starboardChannel = table.Column<ulong>(type: "INTEGER", nullable: true),
                    starboardThreshold = table.Column<int>(type: "INTEGER", nullable: true),
                    starboardEmojiId = table.Column<ulong>(type: "INTEGER", nullable: true),
                    starboardEmojiName = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.id);
                    table.ForeignKey(
                        name: "FK_Configs_guilds_discordId",
                        column: x => x.discordId,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Quotes",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    discordGuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    quotedUserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    UserId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    content = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    timestamp = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Quotes", x => x.id);
                    table.ForeignKey(
                        name: "FK_Quotes_guilds_discordGuildId",
                        column: x => x.discordGuildId,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TrackingDbEntity",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    name = table.Column<string>(type: "TEXT", nullable: false),
                    GuildId = table.Column<long>(type: "INTEGER", nullable: false),
                    creation_epoch = table.Column<long>(type: "INTEGER", nullable: false),
                    channel_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    report_channel = table.Column<ulong>(type: "INTEGER", nullable: false),
                    tracking_regex = table.Column<string>(type: "TEXT", nullable: false),
                    editor_list = table.Column<string>(type: "TEXT", nullable: false),
                    items_flagged = table.Column<uint>(type: "INTEGER", nullable: false),
                    GuildDbEntityId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrackingDbEntity", x => x.id);
                    table.ForeignKey(
                        name: "FK_TrackingDbEntity_guilds_GuildDbEntityId",
                        column: x => x.GuildDbEntityId,
                        principalTable: "guilds",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "AfkStatusEntity",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    afk_message = table.Column<string>(type: "TEXT", maxLength: 70, nullable: true),
                    user_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    afk_epoch = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AfkStatusEntity", x => x.id);
                    table.ForeignKey(
                        name: "FK_AfkStatusEntity_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Incidents",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    guild_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    target_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    moderator_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    reason = table.Column<string>(type: "TEXT", nullable: false),
                    CreationTimeStamp = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Incidents", x => x.id);
                    table.ForeignKey(
                        name: "FK_Incidents_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Incidents_users_target_id",
                        column: x => x.target_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MessageTag",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    tag_name = table.Column<string>(type: "TEXT", nullable: false),
                    tag_data = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    user_id = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageTag", x => x.id);
                    table.ForeignKey(
                        name: "FK_MessageTag_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Reminders",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    userId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    reminderText = table.Column<string>(type: "TEXT", nullable: false),
                    creationTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    executionTime = table.Column<DateTime>(type: "TEXT", nullable: false),
                    isPrivate = table.Column<bool>(type: "INTEGER", nullable: false),
                    channelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    messageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    mentionedChannel = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MentionedMessage = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reminders", x => x.id);
                    table.ForeignKey(
                        name: "FK_Reminders_users_userId",
                        column: x => x.userId,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VoiceAlerts",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    channel_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    guild_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    user_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    is_repeatable = table.Column<bool>(type: "INTEGER", nullable: false),
                    last_alert = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    time_between = table.Column<TimeSpan>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VoiceAlerts", x => x.id);
                    table.ForeignKey(
                        name: "FK_VoiceAlerts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AfkStatusEntity_user_id",
                table: "AfkStatusEntity",
                column: "user_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Configs_discordId",
                table: "Configs",
                column: "discordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_guild_id",
                table: "Incidents",
                column: "guild_id");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_target_id",
                table: "Incidents",
                column: "target_id");

            migrationBuilder.CreateIndex(
                name: "IX_MessageTag_user_id",
                table: "MessageTag",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_Quotes_discordGuildId",
                table: "Quotes",
                column: "discordGuildId");

            migrationBuilder.CreateIndex(
                name: "IX_Reminders_userId",
                table: "Reminders",
                column: "userId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDbEntity_GuildDbEntityId",
                table: "TrackingDbEntity",
                column: "GuildDbEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDbEntity_GuildId",
                table: "TrackingDbEntity",
                column: "GuildId");

            migrationBuilder.CreateIndex(
                name: "IX_TrackingDbEntity_name",
                table: "TrackingDbEntity",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "IX_VoiceAlerts_user_id",
                table: "VoiceAlerts",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AfkStatusEntity");

            migrationBuilder.DropTable(
                name: "Blacklist");

            migrationBuilder.DropTable(
                name: "Configs");

            migrationBuilder.DropTable(
                name: "Incidents");

            migrationBuilder.DropTable(
                name: "MessageTag");

            migrationBuilder.DropTable(
                name: "Quotes");

            migrationBuilder.DropTable(
                name: "Reminders");

            migrationBuilder.DropTable(
                name: "Starboard");

            migrationBuilder.DropTable(
                name: "TrackingDbEntity");

            migrationBuilder.DropTable(
                name: "VoiceAlerts");

            migrationBuilder.DropTable(
                name: "guilds");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
