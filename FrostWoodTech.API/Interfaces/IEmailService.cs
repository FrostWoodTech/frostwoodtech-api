using FrostWoodTech.API.Common;
using FrostWoodTech.API.Email;

namespace FrostWoodTech.API.Interfaces;

public interface IEmailService
{
    /// <summary>Returns a failure instead of throwing, so the caller decides whether a failed send fails the request.</summary>
    Task<ServiceResult<EmailSendResult>> SendAsync(EmailMessage message, CancellationToken cancellationToken);
}
