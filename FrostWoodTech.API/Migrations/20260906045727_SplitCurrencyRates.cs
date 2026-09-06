using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class SplitCurrencyRates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_currencies_rate_positive",
                table: "currencies");

            // Everything anyone has typed so far — USD's pinned 1, and any currency already added
            // (e.g. an admin-typed LKR rate) — becomes exactly what it already was: a manual
            // override. A rename preserves that; a drop+add (what `dotnet ef migrations add`
            // scaffolds by default for a renamed C# property) would have silently discarded it.
            migrationBuilder.RenameColumn(
                name: "rate_from_usd",
                table: "currencies",
                newName: "manual_rate_from_usd");

            migrationBuilder.AlterColumn<decimal>(
                name: "manual_rate_from_usd",
                table: "currencies",
                type: "numeric(18,6)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "live_rate_fetched_at",
                table: "currencies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "live_rate_from_usd",
                table: "currencies",
                type: "numeric(18,6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "live_rate_fetched_at",
                table: "currencies");

            migrationBuilder.DropColumn(
                name: "live_rate_from_usd",
                table: "currencies");

            migrationBuilder.AlterColumn<decimal>(
                name: "manual_rate_from_usd",
                table: "currencies",
                type: "numeric(18,6)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "numeric(18,6)",
                oldNullable: true);

            migrationBuilder.RenameColumn(
                name: "manual_rate_from_usd",
                table: "currencies",
                newName: "rate_from_usd");

            migrationBuilder.AddCheckConstraint(
                name: "ck_currencies_rate_positive",
                table: "currencies",
                sql: "rate_from_usd > 0");
        }
    }
}
