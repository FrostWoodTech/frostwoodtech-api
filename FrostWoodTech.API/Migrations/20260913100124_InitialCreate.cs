using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FrostWoodTech.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
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
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "articles",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    excerpt = table.Column<string>(type: "text", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    content_markdown = table.Column<string>(type: "text", nullable: true),
                    cover_image_key = table.Column<string>(type: "text", nullable: true),
                    slug = table.Column<string>(type: "text", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    show_on_agency = table.Column<bool>(type: "boolean", nullable: false),
                    featured_on_agency = table.Column<bool>(type: "boolean", nullable: false),
                    agency_sort_order = table.Column<int>(type: "integer", nullable: false),
                    show_on_personal = table.Column<bool>(type: "boolean", nullable: false),
                    featured_on_personal = table.Column<bool>(type: "boolean", nullable: false),
                    personal_sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_articles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "certificates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    issued_by = table.Column<string>(type: "text", nullable: false),
                    issued_date = table.Column<DateOnly>(type: "date", nullable: false),
                    marks = table.Column<string>(type: "text", nullable: true),
                    object_key = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: true),
                    height = table.Column<int>(type: "integer", nullable: true),
                    alt_text = table.Column<string>(type: "text", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    featured = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_certificates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "char(3)", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    manual_rate_from_usd = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    live_rate_from_usd = table.Column<decimal>(type: "numeric(18,6)", nullable: true),
                    live_rate_fetched_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "login_attempts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
                    ip_address = table.Column<string>(type: "text", nullable: true),
                    action = table.Column<int>(type: "auth_attempt_action", nullable: false),
                    attempted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_login_attempts", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    tagline = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    price_details = table.Column<string>(type: "text", nullable: true),
                    product_url = table.Column<string>(type: "text", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    seo_title = table.Column<string>(type: "text", nullable: true),
                    seo_description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    show_on_agency = table.Column<bool>(type: "boolean", nullable: false),
                    featured_on_agency = table.Column<bool>(type: "boolean", nullable: false),
                    agency_sort_order = table.Column<int>(type: "integer", nullable: false),
                    show_on_personal = table.Column<bool>(type: "boolean", nullable: false),
                    featured_on_personal = table.Column<bool>(type: "boolean", nullable: false),
                    personal_sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_products", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    year = table.Column<int>(type: "integer", nullable: false),
                    short_description = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    website_url = table.Column<string>(type: "text", nullable: true),
                    problem = table.Column<string>(type: "text", nullable: true),
                    solution = table.Column<string>(type: "text", nullable: true),
                    what_we_delivered = table.Column<string>(type: "text", nullable: true),
                    proof = table.Column<string>(type: "text", nullable: true),
                    client_name = table.Column<string>(type: "text", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    seo_title = table.Column<string>(type: "text", nullable: true),
                    seo_description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    show_on_agency = table.Column<bool>(type: "boolean", nullable: false),
                    featured_on_agency = table.Column<bool>(type: "boolean", nullable: false),
                    agency_sort_order = table.Column<int>(type: "integer", nullable: false),
                    show_on_personal = table.Column<bool>(type: "boolean", nullable: false),
                    featured_on_personal = table.Column<bool>(type: "boolean", nullable: false),
                    personal_sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "reviews",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    position = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    rating = table.Column<int>(type: "integer", nullable: false),
                    review_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    is_featured = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    submitter_ip = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reviews", x => x.id);
                    table.CheckConstraint("ck_reviews_rating_range", "rating BETWEEN 1 AND 5");
                });

            migrationBuilder.CreateTable(
                name: "services",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    short_description = table.Column<string>(type: "text", nullable: false),
                    eyebrow = table.Column<string>(type: "text", nullable: true),
                    headline = table.Column<string>(type: "text", nullable: true),
                    deck = table.Column<string>(type: "text", nullable: true),
                    who_this_is_for = table.Column<string>(type: "text", nullable: true),
                    outcomes = table.Column<string>(type: "text", nullable: true),
                    capabilities = table.Column<string>(type: "text", nullable: true),
                    in_depth = table.Column<string>(type: "text", nullable: true),
                    primary_cta_label = table.Column<string>(type: "text", nullable: true),
                    primary_cta_url = table.Column<string>(type: "text", nullable: true),
                    secondary_cta_label = table.Column<string>(type: "text", nullable: true),
                    secondary_cta_url = table.Column<string>(type: "text", nullable: true),
                    icon_object_key = table.Column<string>(type: "text", nullable: true),
                    icon_url = table.Column<string>(type: "text", nullable: true),
                    icon_width = table.Column<int>(type: "integer", nullable: true),
                    icon_height = table.Column<int>(type: "integer", nullable: true),
                    icon_alt_text = table.Column<string>(type: "text", nullable: true),
                    hero_image_object_key = table.Column<string>(type: "text", nullable: true),
                    hero_image_url = table.Column<string>(type: "text", nullable: true),
                    hero_image_width = table.Column<int>(type: "integer", nullable: true),
                    hero_image_height = table.Column<int>(type: "integer", nullable: true),
                    hero_image_alt_text = table.Column<string>(type: "text", nullable: true),
                    depth_image_object_key = table.Column<string>(type: "text", nullable: true),
                    depth_image_url = table.Column<string>(type: "text", nullable: true),
                    depth_image_width = table.Column<int>(type: "integer", nullable: true),
                    depth_image_height = table.Column<int>(type: "integer", nullable: true),
                    depth_image_alt_text = table.Column<string>(type: "text", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    published_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    seo_title = table.Column<string>(type: "text", nullable: true),
                    seo_description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    show_on_agency = table.Column<bool>(type: "boolean", nullable: false),
                    featured_on_agency = table.Column<bool>(type: "boolean", nullable: false),
                    agency_sort_order = table.Column<int>(type: "integer", nullable: false),
                    show_on_personal = table.Column<bool>(type: "boolean", nullable: false),
                    featured_on_personal = table.Column<bool>(type: "boolean", nullable: false),
                    personal_sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_services", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    is_technology = table.Column<bool>(type: "boolean", nullable: false),
                    technology_category = table.Column<int>(type: "tech_category", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "citext", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: false),
                    last_name = table.Column<string>(type: "text", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    google_subject_id = table.Column<string>(type: "text", nullable: true),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    role = table.Column<int>(type: "user_role", nullable: false),
                    status = table.Column<int>(type: "user_status", nullable: false),
                    approved_by = table.Column<Guid>(type: "uuid", nullable: true),
                    approved_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    rejection_reason = table.Column<string>(type: "text", nullable: true),
                    last_login_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    email_verified_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_users_approved_by",
                        column: x => x.approved_by,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "product_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    product_id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_key = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    alt_text = table.Column<string>(type: "text", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_product_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_product_images_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "project_images",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    object_key = table.Column<string>(type: "text", nullable: false),
                    url = table.Column<string>(type: "text", nullable: false),
                    alt_text = table.Column<string>(type: "text", nullable: false),
                    width = table.Column<int>(type: "integer", nullable: false),
                    height = table.Column<int>(type: "integer", nullable: false),
                    is_primary = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_images", x => x.id);
                    table.ForeignKey(
                        name: "FK_project_images_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "faqs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    question = table.Column<string>(type: "text", nullable: false),
                    answer = table.Column<string>(type: "text", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_agency = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_personal = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_faqs", x => x.id);
                    table.ForeignKey(
                        name: "FK_faqs_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "pricing_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    service_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    tagline = table.Column<string>(type: "text", nullable: true),
                    price_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: true),
                    currency = table.Column<string>(type: "char(3)", nullable: false),
                    price_type = table.Column<int>(type: "price_type", nullable: false),
                    delivery_text = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_popular = table.Column<bool>(type: "boolean", nullable: false),
                    cta_label = table.Column<string>(type: "text", nullable: true),
                    cta_url = table.Column<string>(type: "text", nullable: true),
                    is_published = table.Column<bool>(type: "boolean", nullable: false),
                    featured = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_plans", x => x.id);
                    table.ForeignKey(
                        name: "FK_pricing_plans_services_service_id",
                        column: x => x.service_id,
                        principalTable: "services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "article_tags",
                columns: table => new
                {
                    article_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_article_tags", x => new { x.article_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_article_tags_articles_article_id",
                        column: x => x.article_id,
                        principalTable: "articles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_article_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "project_tags",
                columns: table => new
                {
                    project_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_project_tags", x => new { x.project_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_project_tags_projects_project_id",
                        column: x => x.project_id,
                        principalTable: "projects",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_project_tags_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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

            migrationBuilder.CreateTable(
                name: "email_verification_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_verification_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_verification_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "password_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    purpose = table.Column<int>(type: "password_token_purpose", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_password_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_password_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "refresh_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    replaced_by_token_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "FK_refresh_tokens_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pricing_plan_features",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pricing_plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    is_included = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pricing_plan_features", x => x.id);
                    table.ForeignKey(
                        name: "FK_pricing_plan_features_pricing_plans_pricing_plan_id",
                        column: x => x.pricing_plan_id,
                        principalTable: "pricing_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_article_tags_tag_id",
                table: "article_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_articles_slug",
                table: "articles",
                column: "slug",
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_currencies_code",
                table: "currencies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_token_hash",
                table: "email_verification_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_email_verification_tokens_user_id_created_at",
                table: "email_verification_tokens",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_faqs_service_id",
                table: "faqs",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_login_attempts_email_action_attempted_at",
                table: "login_attempts",
                columns: new[] { "email", "action", "attempted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_login_attempts_ip_address_action_attempted_at",
                table: "login_attempts",
                columns: new[] { "ip_address", "action", "attempted_at" });

            migrationBuilder.CreateIndex(
                name: "IX_password_tokens_token_hash",
                table: "password_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_password_tokens_user_id_purpose_created_at",
                table: "password_tokens",
                columns: new[] { "user_id", "purpose", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_pricing_plan_features_pricing_plan_id",
                table: "pricing_plan_features",
                column: "pricing_plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_pricing_plans_service_id",
                table: "pricing_plans",
                column: "service_id");

            migrationBuilder.CreateIndex(
                name: "IX_product_images_product_id",
                table: "product_images",
                column: "product_id",
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "IX_products_slug",
                table: "products",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_project_images_project_id",
                table: "project_images",
                column: "project_id",
                unique: true,
                filter: "is_primary");

            migrationBuilder.CreateIndex(
                name: "IX_project_tags_tag_id",
                table: "project_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_projects_slug",
                table: "projects",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                table: "refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_user_id",
                table: "refresh_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_service_projects_project_id",
                table: "service_projects",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_services_slug",
                table: "services",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tags_is_technology",
                table: "tags",
                column: "is_technology");

            migrationBuilder.CreateIndex(
                name: "IX_tags_slug",
                table: "tags",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_approved_by",
                table: "users",
                column: "approved_by");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_google_subject_id",
                table: "users",
                column: "google_subject_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_single_super_admin",
                table: "users",
                column: "role",
                unique: true,
                filter: "\"role\" = 'super_admin'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "article_tags");

            migrationBuilder.DropTable(
                name: "certificates");

            migrationBuilder.DropTable(
                name: "contact_submissions");

            migrationBuilder.DropTable(
                name: "currencies");

            migrationBuilder.DropTable(
                name: "email_verification_tokens");

            migrationBuilder.DropTable(
                name: "faqs");

            migrationBuilder.DropTable(
                name: "login_attempts");

            migrationBuilder.DropTable(
                name: "password_tokens");

            migrationBuilder.DropTable(
                name: "pricing_plan_features");

            migrationBuilder.DropTable(
                name: "product_images");

            migrationBuilder.DropTable(
                name: "project_images");

            migrationBuilder.DropTable(
                name: "project_tags");

            migrationBuilder.DropTable(
                name: "refresh_tokens");

            migrationBuilder.DropTable(
                name: "reviews");

            migrationBuilder.DropTable(
                name: "service_projects");

            migrationBuilder.DropTable(
                name: "articles");

            migrationBuilder.DropTable(
                name: "pricing_plans");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "tags");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "services");
        }
    }
}
