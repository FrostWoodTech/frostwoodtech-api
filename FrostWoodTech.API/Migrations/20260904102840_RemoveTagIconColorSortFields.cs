using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTagIconColorSortFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "color_hex",
                table: "tags");

            migrationBuilder.DropColumn(
                name: "icon_object_key",
                table: "tags");

            migrationBuilder.DropColumn(
                name: "icon_url",
                table: "tags");

            migrationBuilder.DropColumn(
                name: "sort_order",
                table: "tags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "color_hex",
                table: "tags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icon_object_key",
                table: "tags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icon_url",
                table: "tags",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "sort_order",
                table: "tags",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
