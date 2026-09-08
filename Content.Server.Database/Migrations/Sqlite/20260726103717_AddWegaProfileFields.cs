using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Content.Server.Database.Migrations.Sqlite
{
    /// <inheritdoc />
    public partial class AddWegaProfileFields : Migration // All the changes here were definitely made to fix the Sqlite database.
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "bark_voice",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "character_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "green_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "yellow_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "red_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "tags_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "links_flavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "nsfwflavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "oocflavor_text",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "profile",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<float>(
                name: "height",
                table: "profile",
                type: "REAL",
                nullable: false,
                defaultValue: 0f);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "bark_voice",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "character_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "green_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "yellow_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "red_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "tags_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "links_flavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "nsfwflavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "oocflavor_text",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "status",
                table: "profile");

            migrationBuilder.DropColumn(
                name: "height",
                table: "profile");
        }
    }
}
