using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class RetirePersonalSite : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "certificates");

            migrationBuilder.DropColumn(
                name: "featured_on_personal",
                table: "services");

            migrationBuilder.DropColumn(
                name: "personal_sort_order",
                table: "services");

            migrationBuilder.DropColumn(
                name: "show_on_agency",
                table: "services");

            migrationBuilder.DropColumn(
                name: "show_on_personal",
                table: "services");

            migrationBuilder.DropColumn(
                name: "featured_on_personal",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "personal_sort_order",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "show_on_agency",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "show_on_personal",
                table: "projects");

            migrationBuilder.DropColumn(
                name: "featured_on_personal",
                table: "products");

            migrationBuilder.DropColumn(
                name: "personal_sort_order",
                table: "products");

            migrationBuilder.DropColumn(
                name: "show_on_agency",
                table: "products");

            migrationBuilder.DropColumn(
                name: "show_on_personal",
                table: "products");

            migrationBuilder.DropColumn(
                name: "show_on_agency",
                table: "faqs");

            migrationBuilder.DropColumn(
                name: "show_on_personal",
                table: "faqs");

            migrationBuilder.DropColumn(
                name: "site",
                table: "contact_submissions");

            migrationBuilder.DropColumn(
                name: "featured_on_personal",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "personal_sort_order",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "show_on_agency",
                table: "articles");

            migrationBuilder.DropColumn(
                name: "show_on_personal",
                table: "articles");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:auth_attempt_action", "login,password_reset")
                .Annotation("Npgsql:Enum:contact_budget_range", "under_one_k,one_to_five_k,five_to_fifteen_k,over_fifteen_k,not_sure")
                .Annotation("Npgsql:Enum:contact_submission_status", "new,read,replied,archived,spam")
                .Annotation("Npgsql:Enum:password_token_purpose", "setup,reset")
                .Annotation("Npgsql:Enum:price_type", "fixed,starting_from,hourly,monthly,custom")
                .Annotation("Npgsql:Enum:tech_category", "frontend,backend,language,database,tool_or_platform,cloud_devops,ai_ml_dl,agentic_ai,design,other")
                .Annotation("Npgsql:Enum:user_role", "super_admin,admin")
                .Annotation("Npgsql:Enum:user_status", "email_verification_required,pending,approved,rejected,disabled")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:auth_attempt_action", "login,password_reset")
                .OldAnnotation("Npgsql:Enum:certificate_category", "course,exam")
                .OldAnnotation("Npgsql:Enum:contact_budget_range", "under_one_k,one_to_five_k,five_to_fifteen_k,over_fifteen_k,not_sure")
                .OldAnnotation("Npgsql:Enum:contact_submission_status", "new,read,replied,archived,spam")
                .OldAnnotation("Npgsql:Enum:password_token_purpose", "setup,reset")
                .OldAnnotation("Npgsql:Enum:price_type", "fixed,starting_from,hourly,monthly,custom")
                .OldAnnotation("Npgsql:Enum:site", "agency,personal")
                .OldAnnotation("Npgsql:Enum:tech_category", "frontend,backend,language,database,tool_or_platform,cloud_devops,ai_ml_dl,agentic_ai,design,other")
                .OldAnnotation("Npgsql:Enum:user_role", "super_admin,admin")
                .OldAnnotation("Npgsql:Enum:user_status", "email_verification_required,pending,approved,rejected,disabled")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:auth_attempt_action", "login,password_reset")
                .Annotation("Npgsql:Enum:certificate_category", "course,exam")
                .Annotation("Npgsql:Enum:contact_budget_range", "under_one_k,one_to_five_k,five_to_fifteen_k,over_fifteen_k,not_sure")
                .Annotation("Npgsql:Enum:contact_submission_status", "new,read,replied,archived,spam")
                .Annotation("Npgsql:Enum:password_token_purpose", "setup,reset")
                .Annotation("Npgsql:Enum:price_type", "fixed,starting_from,hourly,monthly,custom")
                .Annotation("Npgsql:Enum:site", "agency,personal")
                .Annotation("Npgsql:Enum:tech_category", "frontend,backend,language,database,tool_or_platform,cloud_devops,ai_ml_dl,agentic_ai,design,other")
                .Annotation("Npgsql:Enum:user_role", "super_admin,admin")
                .Annotation("Npgsql:Enum:user_status", "email_verification_required,pending,approved,rejected,disabled")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:auth_attempt_action", "login,password_reset")
                .OldAnnotation("Npgsql:Enum:contact_budget_range", "under_one_k,one_to_five_k,five_to_fifteen_k,over_fifteen_k,not_sure")
                .OldAnnotation("Npgsql:Enum:contact_submission_status", "new,read,replied,archived,spam")
                .OldAnnotation("Npgsql:Enum:password_token_purpose", "setup,reset")
                .OldAnnotation("Npgsql:Enum:price_type", "fixed,starting_from,hourly,monthly,custom")
                .OldAnnotation("Npgsql:Enum:tech_category", "frontend,backend,language,database,tool_or_platform,cloud_devops,ai_ml_dl,agentic_ai,design,other")
                .OldAnnotation("Npgsql:Enum:user_role", "super_admin,admin")
                .OldAnnotation("Npgsql:Enum:user_status", "email_verification_required,pending,approved,rejected,disabled")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddColumn<bool>(
                name: "featured_on_personal",
                table: "services",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "personal_sort_order",
                table: "services",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_agency",
                table: "services",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_personal",
                table: "services",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "featured_on_personal",
                table: "projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "personal_sort_order",
                table: "projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_agency",
                table: "projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_personal",
                table: "projects",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "featured_on_personal",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "personal_sort_order",
                table: "products",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_agency",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_personal",
                table: "products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_agency",
                table: "faqs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_personal",
                table: "faqs",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "site",
                table: "contact_submissions",
                type: "site",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "featured_on_personal",
                table: "articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "personal_sort_order",
                table: "articles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_agency",
                table: "articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "show_on_personal",
                table: "articles",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    alt_text = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<int>(type: "certificate_category", nullable: false, defaultValueSql: "'course'"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    deleted_by = table.Column<Guid>(type: "uuid", nullable: true),
                    featured = table.Column<bool>(type: "boolean", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    issued_by = table.Column<string>(type: "text", nullable: false),
                    issued_date = table.Column<DateOnly>(type: "date", nullable: false),
                    marks = table.Column<string>(type: "text", nullable: true),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    object_key = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_certificates_deleted_at",
                table: "certificates",
                column: "deleted_at",
                filter: "is_deleted");
        }
    }
}
