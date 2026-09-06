using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyPricingPlansAgencyOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "agency_sort_order",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "delivery_days",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "featured_on_agency",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "featured_on_personal",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "personal_sort_order",
                table: "pricing_plans");

            migrationBuilder.DropColumn(
                name: "show_on_agency",
                table: "pricing_plans");

            migrationBuilder.RenameColumn(
                name: "show_on_personal",
                table: "pricing_plans",
                newName: "featured");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "featured",
                table: "pricing_plans",
                newName: "show_on_personal");

            migrationBuilder.AddColumn<int>(
                name: "agency_sort_order",
                table: "pricing_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "delivery_days",
                table: "pricing_plans",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "featured_on_agency",
                table: "pricing_plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "featured_on_personal",
                table: "pricing_plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "personal_sort_order",
                table: "pricing_plans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_agency",
                table: "pricing_plans",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
