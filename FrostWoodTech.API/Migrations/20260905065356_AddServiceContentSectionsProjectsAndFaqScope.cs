using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceContentSectionsProjectsAndFaqScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "capabilities",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "deck",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "depth_image_alt_text",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "depth_image_height",
                table: "services",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "depth_image_object_key",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "depth_image_url",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "depth_image_width",
                table: "services",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "eyebrow",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "headline",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "in_depth",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "outcomes",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_cta_label",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "primary_cta_url",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_cta_label",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "secondary_cta_url",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_description",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "seo_title",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "who_this_is_for",
                table: "services",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "service_id",
                table: "faqs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "service_projects",
                columns: table => new
                {
                    service_id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_projects", x => new { x.service_id, x.project_id });
                    table.ForeignKey(
                        name: "FK_service_projects_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_projects_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_faqs_service_id",
                table: "faqs",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_projects_project_id",
                table: "service_projects",
                column: "project_id");

            migrationBuilder.AddForeignKey(
                name: "FK_faqs_services_service_id",
                table: "faqs",
                column: "service_id",
                principalTable: "services",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_faqs_services_service_id",
                table: "faqs");

            migrationBuilder.DropTable(
                name: "service_projects");

            migrationBuilder.DropIndex(
                name: "IX_faqs_service_id",
                table: "faqs");

            migrationBuilder.DropColumn(
                name: "capabilities",
                table: "services");

            migrationBuilder.DropColumn(
                name: "deck",
                table: "services");

            migrationBuilder.DropColumn(
                name: "depth_image_alt_text",
                table: "services");

            migrationBuilder.DropColumn(
                name: "depth_image_height",
                table: "services");

            migrationBuilder.DropColumn(
                name: "depth_image_object_key",
                table: "services");

            migrationBuilder.DropColumn(
                name: "depth_image_url",
                table: "services");

            migrationBuilder.DropColumn(
                name: "depth_image_width",
                table: "services");

            migrationBuilder.DropColumn(
                name: "eyebrow",
                table: "services");

            migrationBuilder.DropColumn(
                name: "headline",
                table: "services");

            migrationBuilder.DropColumn(
                name: "in_depth",
                table: "services");

            migrationBuilder.DropColumn(
                name: "outcomes",
                table: "services");

            migrationBuilder.DropColumn(
                name: "primary_cta_label",
                table: "services");

            migrationBuilder.DropColumn(
                name: "primary_cta_url",
                table: "services");

            migrationBuilder.DropColumn(
                name: "secondary_cta_label",
                table: "services");

            migrationBuilder.DropColumn(
                name: "secondary_cta_url",
                table: "services");

            migrationBuilder.DropColumn(
                name: "seo_description",
                table: "services");

            migrationBuilder.DropColumn(
                name: "seo_title",
                table: "services");

            migrationBuilder.DropColumn(
                name: "who_this_is_for",
                table: "services");

            migrationBuilder.DropColumn(
                name: "service_id",
                table: "faqs");
        }
    }
}
