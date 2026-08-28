using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace NadekoBot.Migrations
{
    /// <inheritdoc />
    public partial class NadekoContextPendingWhatever : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiAgentWhitelistEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ItemId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAgentWhitelistEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StarboardConfig",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Emote = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "⭐"),
                    Threshold = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 3),
                    AllowSelfStar = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    AllowBots = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    Limit = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 100)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarboardConfig", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StarboardEntry",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    StarCount = table.Column<int>(type: "INTEGER", nullable: false),
                    Position = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarboardEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StarboardIgnoredChannel",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    ChannelId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarboardIgnoredChannel", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StarboardMessage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    GuildId = table.Column<ulong>(type: "INTEGER", nullable: false),
                    Index = table.Column<int>(type: "INTEGER", nullable: false),
                    MessageId = table.Column<ulong>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StarboardMessage", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiAgentWhitelistEntry_Type_ItemId",
                table: "AiAgentWhitelistEntry",
                columns: new[] { "Type", "ItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StarboardConfig_GuildId",
                table: "StarboardConfig",
                column: "GuildId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StarboardEntry_GuildId_MessageId",
                table: "StarboardEntry",
                columns: new[] { "GuildId", "MessageId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StarboardEntry_GuildId_Position",
                table: "StarboardEntry",
                columns: new[] { "GuildId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_StarboardIgnoredChannel_GuildId_ChannelId",
                table: "StarboardIgnoredChannel",
                columns: new[] { "GuildId", "ChannelId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StarboardMessage_GuildId_Index",
                table: "StarboardMessage",
                columns: new[] { "GuildId", "Index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAgentWhitelistEntry");

            migrationBuilder.DropTable(
                name: "StarboardConfig");

            migrationBuilder.DropTable(
                name: "StarboardEntry");

            migrationBuilder.DropTable(
                name: "StarboardIgnoredChannel");

            migrationBuilder.DropTable(
                name: "StarboardMessage");
        }
    }
}
