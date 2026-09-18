using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Data;

/// <summary>Seeds the starting tag list. Admins own it afterwards: edits, new tags and deletions all survive a restart.</summary>
public static class TagSeeder
{
    public static async Task EnsureSeededAsync(
        FrostWoodTechDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        // Soft-deleted rows keep their slug, so a tag an admin deleted is never resurrected.
        var taken = await db.Tags
            .IgnoreQueryFilters()
            .Select(t => t.Slug)
            .ToListAsync(cancellationToken);

        var missing = Seeds
            .Where(seed => !taken.Contains(seed.Slug, StringComparer.Ordinal))
            .ToList();

        if (missing.Count == 0)
        {
            return;
        }

        db.Tags.AddRange(missing.Select(seed => new Tag
        {
            Id = Guid.NewGuid(),
            Name = seed.Name,
            Slug = seed.Slug,
            IsTechnology = seed.Category is not null,
            TechnologyCategory = seed.Category
        }));

        try
        {
            await db.SaveChangesAsync(cancellationToken);
            logger.LogWarning("Seeded {Count} tag(s).", missing.Count);
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Tag seeding lost a race — the tags already exist.");
        }
    }

    /// <param name="Category">Null makes it a category tag; anything else makes it a technology tag.</param>
    /// <param name="ExplicitSlug">Only for names the generator would collapse, like C# and .NET.</param>
    private sealed record TagSeed(string Name, TechCategory? Category = null, string? ExplicitSlug = null)
    {
        public string Slug => ExplicitSlug ?? SlugGenerator.Generate(Name);
    }

    private static readonly TagSeed[] Seeds =
    [
        // Category tags: the kind of work a project was, in the words a client would use. The public
        // ?category= filter offers exactly this list.

        // What was built.
        new("Website"),
        new("Web Application"),
        new("Mobile App"),
        new("E-Commerce"),
        new("Admin Dashboard"),
        new("Custom Software"),

        // Business systems.
        new("CRM"),
        new("CMS"),
        new("ERP"),
        new("Booking System"),

        // AI and automation.
        new("AI Integration"),
        new("Machine Learning"),
        new("Business Automation"),

        // Technical.
        new("Frontend"),
        new("Backend"),
        new("Full Stack"),
        new("API Development"),
        new("Cloud & DevOps"),
        new("Third-Party Integrations"),

        new("React", TechCategory.Frontend),
        new("Next.js", TechCategory.Frontend),
        new("Vue.js", TechCategory.Frontend),
        new("Angular", TechCategory.Frontend),
        new("React Native", TechCategory.Frontend),
        new("Blazor", TechCategory.Frontend),
        new("Tailwind CSS", TechCategory.Frontend),
        new("Bootstrap", TechCategory.Frontend),
        new("Redux", TechCategory.Frontend),
        new("Vite", TechCategory.Frontend),
        new("HTML5", TechCategory.Frontend),
        new("CSS3", TechCategory.Frontend),

        new(".NET", TechCategory.Backend, ExplicitSlug: "dotnet"),
        new("Azure Functions", TechCategory.Backend),
        new("Entity Framework Core", TechCategory.Backend),
        new("Node.js", TechCategory.Backend),
        new("Express", TechCategory.Backend),
        new("NestJS", TechCategory.Backend),
        new("Django", TechCategory.Backend),
        new("FastAPI", TechCategory.Backend),
        new("Laravel", TechCategory.Backend),
        new("Spring Boot", TechCategory.Backend),

        new("C#", TechCategory.Language, ExplicitSlug: "csharp"),
        new("TypeScript", TechCategory.Language),
        new("JavaScript", TechCategory.Language),
        new("Python", TechCategory.Language),
        new("Java", TechCategory.Language),
        new("PHP", TechCategory.Language),
        new("Go", TechCategory.Language),
        new("Dart", TechCategory.Language),
        new("Kotlin", TechCategory.Language),
        new("Swift", TechCategory.Language),
        new("SQL", TechCategory.Language),

        new("PostgreSQL", TechCategory.Database),
        new("Neon", TechCategory.Database),
        new("SQL Server", TechCategory.Database),
        new("MySQL", TechCategory.Database),
        new("MongoDB", TechCategory.Database),
        new("Redis", TechCategory.Database),
        new("SQLite", TechCategory.Database),

        new("Git", TechCategory.ToolOrPlatform),
        new("GitHub", TechCategory.ToolOrPlatform),
        new("Docker", TechCategory.ToolOrPlatform),
        new("Postman", TechCategory.ToolOrPlatform),
        new("Stripe", TechCategory.ToolOrPlatform),
        new("WordPress", TechCategory.ToolOrPlatform),
        new("Shopify", TechCategory.ToolOrPlatform),
        new("Jira", TechCategory.ToolOrPlatform),
        new("n8n", TechCategory.ToolOrPlatform),

        new("Azure", TechCategory.CloudDevops),
        new("AWS", TechCategory.CloudDevops),
        new("Google Cloud", TechCategory.CloudDevops),
        new("Azure DevOps", TechCategory.CloudDevops),
        new("GitHub Actions", TechCategory.CloudDevops),
        new("Kubernetes", TechCategory.CloudDevops),
        new("Terraform", TechCategory.CloudDevops),
        new("Cloudflare", TechCategory.CloudDevops),
        new("Vercel", TechCategory.CloudDevops),
        new("Netlify", TechCategory.CloudDevops),

        new("TensorFlow", TechCategory.AiMlDl),
        new("PyTorch", TechCategory.AiMlDl),
        new("scikit-learn", TechCategory.AiMlDl),
        new("OpenAI API", TechCategory.AiMlDl),
        new("Hugging Face", TechCategory.AiMlDl),
        new("Computer Vision", TechCategory.AiMlDl),
        new("NLP", TechCategory.AiMlDl),

        new("Claude API", TechCategory.AgenticAi),
        new("LangChain", TechCategory.AgenticAi),
        new("LangGraph", TechCategory.AgenticAi),
        new("Model Context Protocol", TechCategory.AgenticAi),
        new("Retrieval-Augmented Generation", TechCategory.AgenticAi),
        new("Semantic Kernel", TechCategory.AgenticAi),

        new("Figma", TechCategory.Design),
        new("Adobe XD", TechCategory.Design),
        new("Photoshop", TechCategory.Design),
        new("Illustrator", TechCategory.Design),
        new("Canva", TechCategory.Design),

        new("REST API", TechCategory.Other),
        new("GraphQL", TechCategory.Other),
        new("WebSockets", TechCategory.Other),
        new("SEO", TechCategory.Other),
        new("Accessibility", TechCategory.Other),
        new("Responsive Design", TechCategory.Other)
    ];
}
