using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class AddContactSubmissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:auth_attempt_action", "login,password_reset")
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
                .OldAnnotation("Npgsql:Enum:password_token_purpose", "setup,reset")
                .OldAnnotation("Npgsql:Enum:price_type", "fixed,starting_from,hourly,monthly,custom")
                .OldAnnotation("Npgsql:Enum:tech_category", "frontend,backend,language,database,tool_or_platform,cloud_devops,ai_ml_dl,agentic_ai,design,other")
                .OldAnnotation("Npgsql:Enum:user_role", "super_admin,admin")
                .OldAnnotation("Npgsql:Enum:user_status", "email_verification_required,pending,approved,rejected,disabled")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "contact_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    company = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    subject = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    budget_range = table.Column<int>(type: "contact_budget_range", nullable: true),
                    site = table.Column<int>(type: "site", nullable: false),
                    status = table.Column<int>(type: "contact_submission_status", nullable: false),
                    admin_notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    replied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replied_by = table.Column<Guid>(type: "uuid", nullable: true),
                    submitter_ip = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_submissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_contact_submissions_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_contact_submissions_users_replied_by",
                        column: x => x.replied_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_contact_submissions_replied_by",
                table: "contact_submissions",
                column: "replied_by");

            migrationBuilder.CreateIndex(
                name: "IX_contact_submissions_service_id",
                table: "contact_submissions",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "ix_contact_submissions_status_created_at",
                table: "contact_submissions",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_contact_submissions_submitter_ip_created_at",
                table: "contact_submissions",
                columns: new[] { "submitter_ip", "created_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_submissions");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:auth_attempt_action", "login,password_reset")
                .Annotation("Npgsql:Enum:password_token_purpose", "setup,reset")
                .Annotation("Npgsql:Enum:price_type", "fixed,starting_from,hourly,monthly,custom")
                .Annotation("Npgsql:Enum:tech_category", "frontend,backend,language,database,tool_or_platform,cloud_devops,ai_ml_dl,agentic_ai,design,other")
                .Annotation("Npgsql:Enum:user_role", "super_admin,admin")
                .Annotation("Npgsql:Enum:user_status", "email_verification_required,pending,approved,rejected,disabled")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:auth_attempt_action", "login,password_reset")
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
    }
}
