using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Common;
using FrostWoodTech.API.Email;
using FrostWoodTech.API.Interfaces;

namespace FrostWoodTech.API.Services;

/// <summary>Default transport: validates and logs the message instead of sending. For local dev and CI.</summary>
public class LoggingEmailService : IEmailService
{
    private readonly EmailOptions _options;
    private readonly ILogger<LoggingEmailService> _logger;

    public LoggingEmailService(IOptions<EmailOptions> options, ILogger<LoggingEmailService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public Task<ServiceResult<EmailSendResult>> SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(message);

        var validation = Validate(message);
        if (validation is not null)
        {
            return Task.FromResult(validation);
        }

        // Logs full links, so never use this transport in a deployment.
        _logger.LogInformation(
            "Email not sent (provider 'log'). To: {To}, Subject: {Subject}\n{Body}",
            message.To,
            message.Subject,
            message.TextBody ?? message.HtmlBody);

        return Task.FromResult(ServiceResult<EmailSendResult>.Success(new EmailSendResult
        {
            Provider = "log"
        }));
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
}
