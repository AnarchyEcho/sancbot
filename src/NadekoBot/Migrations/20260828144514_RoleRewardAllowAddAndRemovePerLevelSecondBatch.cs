using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    /// <inheritdoc />
    public partial class RoleRewardAllowAddAndRemovePerLevelSecondBatch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_XpRoleReward_XpSettingsId_Level",
                table: "XpRoleReward");

            migrationBuilder.CreateIndex(
                name: "IX_XpRoleReward_XpSettingsId_Level_Remove",
                table: "XpRoleReward",
                columns: new[] { "XpSettingsId", "Level", "Remove" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_XpRoleReward_XpSettingsId_Level_Remove",
                table: "XpRoleReward");

            migrationBuilder.CreateIndex(
                name: "IX_XpRoleReward_XpSettingsId_Level",
                table: "XpRoleReward",
                columns: new[] { "XpSettingsId", "Level" },
                unique: true);
        }
    }
}
