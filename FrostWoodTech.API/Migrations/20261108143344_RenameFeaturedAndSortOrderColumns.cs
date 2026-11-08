using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class RenameFeaturedAndSortOrderColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "featured_on_agency",
                table: "services",
                newName: "is_featured");

            migrationBuilder.RenameColumn(
                name: "agency_sort_order",
                table: "services",
                newName: "sort_order");

            migrationBuilder.RenameColumn(
                name: "featured_on_agency",
                table: "projects",
                newName: "is_featured");

            migrationBuilder.RenameColumn(
                name: "agency_sort_order",
                table: "projects",
                newName: "sort_order");

            migrationBuilder.RenameColumn(
                name: "featured_on_agency",
                table: "products",
                newName: "is_featured");

            migrationBuilder.RenameColumn(
                name: "agency_sort_order",
                table: "products",
                newName: "sort_order");

            migrationBuilder.RenameColumn(
                name: "featured_on_agency",
                table: "articles",
                newName: "is_featured");

            migrationBuilder.RenameColumn(
                name: "agency_sort_order",
                table: "articles",
                newName: "sort_order");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "services",
                newName: "agency_sort_order");

            migrationBuilder.RenameColumn(
                name: "is_featured",
                table: "services",
                newName: "featured_on_agency");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "projects",
                newName: "agency_sort_order");

            migrationBuilder.RenameColumn(
                name: "is_featured",
                table: "projects",
                newName: "featured_on_agency");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "products",
                newName: "agency_sort_order");

            migrationBuilder.RenameColumn(
                name: "is_featured",
                table: "products",
                newName: "featured_on_agency");

            migrationBuilder.RenameColumn(
                name: "sort_order",
                table: "articles",
                newName: "agency_sort_order");

            migrationBuilder.RenameColumn(
                name: "is_featured",
                table: "articles",
                newName: "featured_on_agency");
        }
    }
}
