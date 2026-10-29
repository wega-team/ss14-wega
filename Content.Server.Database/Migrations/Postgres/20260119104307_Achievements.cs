using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Content.Server.Database.Migrations.Postgres
{
    /// <inheritdoc />
    public partial class Achievements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "achievement",
                columns: table => new
                {
                    achievement_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    player_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    achievement_key = table.Column<byte>(type: "smallint", nullable: false),
                    unlocked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_achievement", x => x.achievement_id);
                    table.ForeignKey(
                        name: "FK_achievement_player_player_user_id",
                        column: x => x.player_user_id,
                        principalTable: "player",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_achievement_player_user_id",
                table: "achievement",
                column: "player_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_achievement_player_user_id_achievement_key",
                table: "achievement",
                columns: new[] { "player_user_id", "achievement_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "achievement");
        }
    }
}
