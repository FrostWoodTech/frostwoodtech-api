namespace FrostWoodTech.API.Enums;

public static class TechCategoryLabels
{
    public static string For(TechCategory category) => category switch
    {
        TechCategory.Frontend => "Frontend",
        TechCategory.Backend => "Backend",
        TechCategory.Language => "Language",
        TechCategory.Database => "Database",
        TechCategory.ToolOrPlatform => "Tool / platform",
        TechCategory.CloudDevops => "Cloud & DevOps",
        TechCategory.AiMlDl => "AI / ML / DL",
        TechCategory.AgenticAi => "Agentic AI",
        TechCategory.Design => "Design",
        TechCategory.Other => "Other",
        _ => category.ToString()
    };
}
