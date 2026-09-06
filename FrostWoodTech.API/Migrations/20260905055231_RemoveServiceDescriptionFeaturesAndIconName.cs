using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceDescriptionFeaturesAndIconName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "service_features");

            migrationBuilder.DropColumn(
                name: "description",
                table: "services");

            migrationBuilder.DropColumn(
                name: "hero_image_id",
                table: "services");

            migrationBuilder.DropColumn(
                name: "icon_name",
                table: "services");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "description",
                table: "services",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "hero_image_id",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "icon_name",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "service_features",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    icon_name = table.Column<string>(type: "text", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_features", x => x.id);
                    table.ForeignKey(
                        name: "FK_service_features_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_service_features_service_id",
                table: "service_features",
                column: "service_id");
        }
    }
}
