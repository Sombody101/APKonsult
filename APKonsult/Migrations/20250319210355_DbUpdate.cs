using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APKonsult.Migrations
{
    /// <inheritdoc />
    public partial class DbUpdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "starboardThreshold",
                table: "Configs",
                newName: "starboard_threshold");

            migrationBuilder.RenameColumn(
                name: "starboardEnabled",
                table: "Configs",
                newName: "starboard_enabled");

            migrationBuilder.RenameColumn(
                name: "starboardEmojiName",
                table: "Configs",
                newName: "starboard_emoji_name");

            migrationBuilder.RenameColumn(
                name: "starboardEmojiId",
                table: "Configs",
                newName: "starboard_emoji_id");

            migrationBuilder.RenameColumn(
                name: "starboardChannel",
                table: "Configs",
                newName: "starboard_channel");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "starboard_threshold",
                table: "Configs",
                newName: "starboardThreshold");

            migrationBuilder.RenameColumn(
                name: "starboard_enabled",
                table: "Configs",
                newName: "starboardEnabled");

            migrationBuilder.RenameColumn(
                name: "starboard_emoji_name",
                table: "Configs",
                newName: "starboardEmojiName");

            migrationBuilder.RenameColumn(
                name: "starboard_emoji_id",
                table: "Configs",
                newName: "starboardEmojiId");

            migrationBuilder.RenameColumn(
                name: "starboard_channel",
                table: "Configs",
                newName: "starboardChannel");
        }
    }
}
