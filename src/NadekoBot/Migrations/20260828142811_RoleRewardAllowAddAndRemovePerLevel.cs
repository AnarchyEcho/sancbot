using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    /// <inheritdoc />
    public partial class RoleRewardAllowAddAndRemovePerLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PrevRoles",
                table: "MutedUserId",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrevRoles",
                table: "MutedUserId");
        }
    }
}
