using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Public;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Home;

public class GetPublicHome
{
    private const int SliceSize = 6;

    private const int FaqSliceSize = 20;

    private readonly IProjectService _projectService;
    private readonly IArticleService _articleService;
    private readonly IServiceCatalogService _serviceCatalogService;
    private readonly IPricingService _pricingService;
    private readonly IFaqService _faqService;
    private readonly IReviewService _reviewService;

    public GetPublicHome(
        IProjectService projectService,
        IArticleService articleService,
        IServiceCatalogService serviceCatalogService,
        IPricingService pricingService,
        IFaqService faqService,
        IReviewService reviewService)
    {
        _projectService = projectService;
        _articleService = articleService;
        _serviceCatalogService = serviceCatalogService;
        _pricingService = pricingService;
        _faqService = faqService;
        _reviewService = reviewService;
    }

    [Function("GetPublicHome")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "public/home")] HttpRequest req,
        CancellationToken cancellationToken)
    {
        // Sequential on purpose: the services share one scoped DbContext, which isn't thread-safe.
        var projects = await _projectService.GetPublicProjectsAsync(
            tagSlug: null,
            categorySlug: null,
            featured: true,
            page: 1,
            pageSize: SliceSize,
            cancellationToken);

        var articles = await _articleService.GetPublicArticlesAsync(
            tagSlug: null,
            featured: true,
            page: 1,
            pageSize: SliceSize,
            cancellationToken);

        var services = await _serviceCatalogService.GetPublicServicesAsync(
            featured: true,
            page: 1,
            pageSize: SliceSize,
            cancellationToken);

        var pricingPlans = await _pricingService.GetPublicComboPlansAsync(
            featured: true,
            page: 1,
            pageSize: SliceSize,
            cancellationToken);

        var faqs = await _faqService.GetPublicFaqsAsync(cancellationToken);

        var reviews = await _reviewService.GetFeaturedForHomeAsync(SliceSize, cancellationToken);

        var payload = new HomeResponse
        {
            FeaturedProjects = projects.Items,
            FeaturedArticles = articles.Items,
            FeaturedServices = services.Items,
            FeaturedPricingPlans = pricingPlans.Items,
            Faqs = [.. faqs.Take(FaqSliceSize)],
            FeaturedReviews = reviews
        };

        return HttpResponses.PublicJson(req, payload);
    }
}
