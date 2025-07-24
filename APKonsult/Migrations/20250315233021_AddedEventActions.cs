using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APKonsult.Migrations
{
    /// <inheritdoc />
    public partial class AddedEventActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventAction",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    event_name = table.Column<string>(type: "TEXT", nullable: false),
                    guild_id = table.Column<ulong>(type: "INTEGER", nullable: false),
                    lua_script = table.Column<string>(type: "TEXT", nullable: false),
                    action_name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventAction", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventAction_guilds_guild_id",
                        column: x => x.guild_id,
                        principalTable: "guilds",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventAction_guild_id",
                table: "EventAction",
                column: "guild_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventAction");
        }
    }
}
