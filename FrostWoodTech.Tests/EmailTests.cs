using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using FrostWoodTech.API.Email;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

public class EmailTests
{
    [Fact]
    public async Task A_well_formed_message_is_accepted()
    {
        var result = await CreateService().SendAsync(NewMessage(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("log", result.Value!.Provider);
    }

    [Fact]
    public async Task A_message_without_a_recipient_is_a_validation_failure()
    {
        var message = NewMessage();
        message.To = "   ";

        var result = await CreateService().SendAsync(message, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation_failed", result.Error!.Code);
    }

    [Fact]
    public async Task A_missing_from_address_is_reported_rather_than_thrown()
    {
        var service = CreateService(fromAddress: string.Empty);

        var result = await service.SendAsync(NewMessage(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Email__FromAddress", result.Error!.Message, StringComparison.Ordinal);
    }

    private static LoggingEmailService CreateService(string fromAddress = "no-reply@example.test") =>
        new(
            Options.Create(new EmailOptions { FromAddress = fromAddress }),
            NullLogger<LoggingEmailService>.Instance);

    private static EmailMessage NewMessage() => new()
    {
        To = "admin@example.test",
        Subject = "Reset your password",
        HtmlBody = "<p>Use this link within 15 minutes.</p>"
    };
}
