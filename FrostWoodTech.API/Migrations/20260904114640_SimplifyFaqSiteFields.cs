using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyFaqSiteFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agency_sort_order",
                table: "faqs");

            migrationBuilder.DropColumn(
                name: "featured_on_agency",
                table: "faqs");

            migrationBuilder.DropColumn(
                name: "featured_on_personal",
                table: "faqs");

            migrationBuilder.DropColumn(
                name: "personal_sort_order",
                table: "faqs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "agency_sort_order",
                table: "faqs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "featured_on_agency",
                table: "faqs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "featured_on_personal",
                table: "faqs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "personal_sort_order",
                table: "faqs",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
