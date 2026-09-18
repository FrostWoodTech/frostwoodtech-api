using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Functions.Articles;
using FrostWoodTech.API.Functions.Faqs;
using FrostWoodTech.API.Functions.Home;
using FrostWoodTech.API.Functions.Products;
using FrostWoodTech.API.Functions.Projects;
using FrostWoodTech.API.Functions.Services;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.Tests;

/// <summary>The site check lives in the Functions, so each one is called directly with a stub service.</summary>
public class PublicSiteGuardTests
{
    public static TheoryData<string> Endpoints =>
    [
        "projects", "project-by-slug", "products", "product-by-slug", "articles", "article-by-slug",
        "services", "service-by-slug", "faqs", "home"
    ];

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task A_missing_site_is_a_400_and_the_service_is_never_called(string endpoint)
    {
        var (run, recorder) = Build(endpoint);

        var result = await run(ApiAssert.Request());

        ApiAssert.Problem(result, StatusCodes.Status400BadRequest, "site_required");
        Assert.Empty(recorder.SitesReceived);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task An_unknown_site_is_a_400_and_the_service_is_never_called(string endpoint)
    {
        var (run, recorder) = Build(endpoint);

        var result = await run(ApiAssert.Request("site=99"));

        ApiAssert.Problem(result, StatusCodes.Status400BadRequest, "validation_failed");
        Assert.Empty(recorder.SitesReceived);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task A_valid_site_reaches_the_service_unchanged(string endpoint)
    {
        var (run, recorder) = Build(endpoint);

        var result = await run(ApiAssert.Request("site=personal"));

        Assert.IsType<ContentResult>(result);
        Assert.NotEmpty(recorder.SitesReceived);
        Assert.All(recorder.SitesReceived, site => Assert.Equal(Site.Personal, site));
    }

    private static (Func<HttpRequest, Task<IActionResult>> Run, RecordingServiceProxy Recorder) Build(string endpoint)
    {
        var none = CancellationToken.None;

        switch (endpoint)
        {
            case "projects":
            {
                var (service, recorder) = RecordingServiceProxy.Create<IProjectService>();
                return (req => new GetPublicProjects(service).Run(req, none), recorder);
            }
            case "project-by-slug":
            {
                var (service, recorder) = RecordingServiceProxy.Create<IProjectService>();
                return (req => new GetPublicProjectBySlug(service).Run(req, "a-slug", none), recorder);
            }
            case "products":
            {
                var (service, recorder) = RecordingServiceProxy.Create<IProductService>();
                return (req => new GetPublicProducts(service).Run(req, none), recorder);
            }
            case "product-by-slug":
            {
                var (service, recorder) = RecordingServiceProxy.Create<IProductService>();
                return (req => new GetPublicProductBySlug(service).Run(req, "a-slug", none), recorder);
            }
            case "articles":
            {
                var (service, recorder) = RecordingServiceProxy.Create<IArticleService>();
                return (req => new GetPublicArticles(service).Run(req, none), recorder);
            }
            case "article-by-slug":
            {
                var (service, recorder) = RecordingServiceProxy.Create<IArticleService>();
                return (req => new GetPublicArticleBySlug(service).Run(req, "a-slug", none), recorder);
            }
            case "services":
            {
                var (service, recorder) = RecordingServiceProxy.Create<IServiceCatalogService>();
                return (req => new GetPublicServices(service).Run(req, none), recorder);
            }
            case "service-by-slug":
            {
                var (service, recorder) = RecordingServiceProxy.Create<IServiceCatalogService>();
                return (req => new GetPublicServiceBySlug(service).Run(req, "a-slug", none), recorder);
            }
            case "faqs":
            {
                var (service, recorder) = RecordingServiceProxy.Create<IFaqService>();
                return (req => new GetPublicFaqs(service).Run(req, none), recorder);
            }
            case "home":
            {
                var (projects, recorder) = RecordingServiceProxy.Create<IProjectService>();
                var (articles, articleRecorder) = RecordingServiceProxy.Create<IArticleService>();
                var (services, serviceRecorder) = RecordingServiceProxy.Create<IServiceCatalogService>();
                var (pricing, _) = RecordingServiceProxy.Create<IPricingService>();
                var (faqs, faqRecorder) = RecordingServiceProxy.Create<IFaqService>();
                var (reviews, _) = RecordingServiceProxy.Create<IReviewService>();

                return (async req =>
                {
                    var result = await new GetPublicHome(projects, articles, services, pricing, faqs, reviews).Run(req, none);
                    recorder.SitesReceived.AddRange(articleRecorder.SitesReceived);
                    recorder.SitesReceived.AddRange(serviceRecorder.SitesReceived);
                    recorder.SitesReceived.AddRange(faqRecorder.SitesReceived);

                    return result;
                }, recorder);
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(endpoint), endpoint, null);
        }
    }
}
