using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceIconAndHeroImageUploads : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "hero_image_alt_text",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "hero_image_height",
                table: "services",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hero_image_object_key",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "hero_image_url",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "hero_image_width",
                table: "services",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icon_alt_text",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "icon_height",
                table: "services",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icon_url",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "icon_width",
                table: "services",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hero_image_alt_text",
                table: "services");

            migrationBuilder.DropColumn(
                name: "hero_image_height",
                table: "services");

            migrationBuilder.DropColumn(
                name: "hero_image_object_key",
                table: "services");

            migrationBuilder.DropColumn(
                name: "hero_image_url",
                table: "services");

            migrationBuilder.DropColumn(
                name: "hero_image_width",
                table: "services");

            migrationBuilder.DropColumn(
                name: "icon_alt_text",
                table: "services");

            migrationBuilder.DropColumn(
                name: "icon_height",
                table: "services");

            migrationBuilder.DropColumn(
                name: "icon_url",
                table: "services");

            migrationBuilder.DropColumn(
                name: "icon_width",
                table: "services");
        }
    }
}
