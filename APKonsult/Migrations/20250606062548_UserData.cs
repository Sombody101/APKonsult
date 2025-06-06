using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APKonsult.Migrations
{
    /// <inheritdoc />
    public partial class UserData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Flags",
                table: "users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OAuthFlags",
                table: "users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "avatar_hash",
                table: "users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "banner_hash",
                table: "users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "discriminator",
                table: "users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "email",
                table: "users",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "global_name",
                table: "users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "is_bot",
                table: "users",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "locale",
                table: "users",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "mfa_enabled",
                table: "users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "premium_type",
                table: "users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "verified",
                table: "users",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "DocketItemEntity",
                columns: table => new
                {
                    id = table.Column<ulong>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    item_name = table.Column<string>(type: "TEXT", nullable: false),
                    description = table.Column<string>(type: "TEXT", nullable: false),
                    status = table.Column<int>(type: "INTEGER", nullable: false),
                    close_reason = table.Column<string>(type: "TEXT", nullable: false),
                    GuildDbEntityId = table.Column<ulong>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocketItemEntity", x => x.id);
                    table.ForeignKey(
                        name: "FK_DocketItemEntity_guilds_GuildDbEntityId",
                        column: x => x.GuildDbEntityId,
                        principalTable: "guilds",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DocketItemEntity_GuildDbEntityId",
                table: "DocketItemEntity",
                column: "GuildDbEntityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DocketItemEntity");

            migrationBuilder.DropColumn(
                name: "Flags",
                table: "users");

            migrationBuilder.DropColumn(
                name: "OAuthFlags",
                table: "users");

            migrationBuilder.DropColumn(
                name: "avatar_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "banner_hash",
                table: "users");

            migrationBuilder.DropColumn(
                name: "discriminator",
                table: "users");

            migrationBuilder.DropColumn(
                name: "email",
                table: "users");

            migrationBuilder.DropColumn(
                name: "global_name",
                table: "users");

            migrationBuilder.DropColumn(
                name: "is_bot",
                table: "users");

            migrationBuilder.DropColumn(
                name: "locale",
                table: "users");

            migrationBuilder.DropColumn(
                name: "mfa_enabled",
                table: "users");

            migrationBuilder.DropColumn(
                name: "premium_type",
                table: "users");

            migrationBuilder.DropColumn(
                name: "verified",
                table: "users");
        }
    }
}
