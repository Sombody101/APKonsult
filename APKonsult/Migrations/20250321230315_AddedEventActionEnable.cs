using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace APKonsult.Migrations
{
    /// <inheritdoc />
    public partial class AddedEventActionEnable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "enabled",
                table: "EventAction",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "enabled",
                table: "EventAction");
        }
    }
}
