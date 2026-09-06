using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Functions.Media;

public class GetMediaConfig
{
    private readonly IMediaService _mediaService;

    public GetMediaConfig(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    /// <summary>Hands the admin SPA the base URL it needs to render a round-tripped `media://` token.</summary>
    [Function("GetMediaConfig")]
    public IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "cms/admin/media/config")] HttpRequest req)
    {
        HttpResponses.MarkNoStore(req);

        return new OkObjectResult(new MediaConfigResponse
        {
            PublicBaseUrl = _mediaService.GetPublicBaseUrl()
        });
    }
}
