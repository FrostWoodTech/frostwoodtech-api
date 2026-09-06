using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceArticleDatesRemoveMediumUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "medium_url",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "published_date",
                table: "articles");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "published_at",
                table: "articles",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "published_at",
                table: "articles");

            migrationBuilder.AddColumn<string>(
                name: "medium_url",
                table: "articles",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "published_date",
                table: "articles",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1));
        }
    }
}
