using System.Net;
using System.Text;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Email;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

public class BrevoEmailServiceTests
{
    [Fact]
    public async Task A_successful_send_returns_the_provider_message_id()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.Created, """{"messageId":"abc-123"}""");
        var service = CreateService(handler);

        var result = await service.SendAsync(NewMessage(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("brevo", result.Value!.Provider);
        Assert.Equal("abc-123", result.Value!.MessageId);
        Assert.Equal("api-key", handler.LastRequest!.Headers.GetValues("api-key").Single());
    }

    [Fact]
    public async Task A_non_success_response_is_reported_rather_than_thrown()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.BadRequest, """{"message":"invalid sender"}""");
        var service = CreateService(handler);

        var result = await service.SendAsync(NewMessage(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task A_message_without_a_recipient_is_a_validation_failure_before_any_network_call()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, """{"messageId":"unused"}""");
        var service = CreateService(handler);

        var message = NewMessage();
        message.To = "   ";

        var result = await service.SendAsync(message, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
        Assert.Null(handler.LastRequest);
    }

    private static BrevoEmailService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.brevo.com/v3/") };

        return new BrevoEmailService(
            httpClient,
            Options.Create(new EmailOptions
            {
                FromAddress = "no-reply@example.test",
                FromName = "FrostWoodTech",
                ApiKey = "api-key"
            }),
            NullLogger<BrevoEmailService>.Instance);
    }

    private static EmailMessage NewMessage() => new()
    {
        To = "admin@example.test",
        Subject = "Reset your password",
        HtmlBody = "<p>Use this link within 15 minutes.</p>"
    };

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string responseBody) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            LastRequest = request;

            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            });
        }
    }
}
