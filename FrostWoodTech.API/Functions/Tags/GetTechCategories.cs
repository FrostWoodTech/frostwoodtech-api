using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Functions.Tags;

/// <summary>Fixed lookup list — lets the admin SPA build its category picker without hardcoding the enum.</summary>
public class GetTechCategories
{
    [Function("GetTechCategories")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/tags/categories")] HttpRequest req)
    {
        HttpResponses.MarkNoStore(req);

        var options = Enum.GetValues<TechCategory>()
            .Select(category => new TechCategoryOption { Value = category, Label = TechCategoryLabels.For(category) })
            .ToList();

        return new OkObjectResult(options);
    }
}
