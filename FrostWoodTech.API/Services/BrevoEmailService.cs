using System.Net.Http.Json;
using System.Text.Json.Serialization;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

/// <summary>Sends via Brevo's transactional API; validation matches LoggingEmailService.</summary>
public class BrevoEmailService : IEmailService
{
    private readonly HttpClient _httpClient;
    private readonly EmailOptions _options;
    private readonly ILogger<BrevoEmailService> _logger;

    public BrevoEmailService(HttpClient httpClient, IOptions<EmailOptions> options, ILogger<BrevoEmailService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<ServiceResult<EmailSendResult>> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var validation = Validate(message);
        if (validation is not null)
        {
            return validation;
        }

        var payload = new BrevoSendRequest
        {
            Sender = new BrevoContact { Email = _options.FromAddress, Name = _options.FromName },
            To = [new BrevoContact { Email = message.To }],
            Subject = message.Subject,
            HtmlContent = message.HtmlBody,
            TextContent = message.TextBody,
            ReplyTo = message.ReplyTo is null ? null : new BrevoContact { Email = message.ReplyTo }
        };

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("api-key", _options.ApiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Brevo rejected the message. Status: {Status}, Body: {Body}",
                    (int)response.StatusCode,
                    body);

                return ServiceResult<EmailSendResult>.Validation(
                    $"Brevo could not send the message ({(int)response.StatusCode}).");
            }

            var parsed = System.Text.Json.JsonSerializer.Deserialize<BrevoSendResponse>(body);

            return ServiceResult<EmailSendResult>.Success(new EmailSendResult
            {
                MessageId = parsed?.MessageId,
                Provider = "brevo"
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogError(ex, "Brevo send failed. To: {To}, Subject: {Subject}", message.To, message.Subject);

            return ServiceResult<EmailSendResult>.Validation("Email could not be sent. The provider is unreachable.");
        }
    }

    private ServiceResult<EmailSendResult>? Validate(EmailMessage message)
    {
        if (!_options.IsConfigured)
        {
            return ServiceResult<EmailSendResult>.Validation(
                "Email is not configured. Set Email__FromAddress.");
        }

        if (string.IsNullOrWhiteSpace(message.To) || !message.To.Contains('@', StringComparison.Ordinal))
        {
            return ServiceResult<EmailSendResult>.Validation("A valid recipient address is required.");
        }

        if (string.IsNullOrWhiteSpace(message.Subject))
        {
            return ServiceResult<EmailSendResult>.Validation("subject is required.");
        }

        return string.IsNullOrWhiteSpace(message.HtmlBody)
            ? ServiceResult<EmailSendResult>.Validation("htmlBody is required.")
            : null;
    }

    private sealed class BrevoSendRequest
    {
        [JsonPropertyName("sender")]
        public required BrevoContact Sender { get; init; }

        [JsonPropertyName("to")]
        public required List<BrevoContact> To { get; init; }

        [JsonPropertyName("subject")]
        public required string Subject { get; init; }

        [JsonPropertyName("htmlContent")]
        public required string HtmlContent { get; init; }

        [JsonPropertyName("textContent")]
        public string? TextContent { get; init; }

        [JsonPropertyName("replyTo")]
        public BrevoContact? ReplyTo { get; init; }
    }

    private sealed class BrevoContact
    {
        [JsonPropertyName("email")]
        public required string Email { get; init; }

        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }

    private sealed class BrevoSendResponse
    {
        [JsonPropertyName("messageId")]
        public string? MessageId { get; init; }
    }
}
