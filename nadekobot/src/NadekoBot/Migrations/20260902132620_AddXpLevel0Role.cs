using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    /// <inheritdoc />
    public partial class AddXpLevel0Role : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<ulong>(
                name: "Level0RoleId",
                table: "XpSettings",
                type: "INTEGER",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Level0RoleId",
                table: "XpSettings");
        }
    }
}
