using FrostWoodTech.API.Common;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Enums;

namespace FrostWoodTech.API.Interfaces;

public interface IUserService
{
    /// <summary>Creates an email_verification_required account; issues no tokens.</summary>
    Task<ServiceResult<AdminUserResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    /// <summary>Moves the account to pending; issues no tokens.</summary>
    Task<ServiceResult<AdminUserResponse>> VerifyEmailAsync(
        VerifyEmailRequest request,
        CancellationToken cancellationToken);

    /// <summary>Always the same generic response, so it can't be used to probe emails.</summary>
    Task<ServiceResult<ResendVerificationResponse>> ResendVerificationAsync(
        ResendVerificationRequest request,
        CancellationToken cancellationToken);

    /// <summary>Always the same generic response. Invalidates older reset links first.</summary>
    Task<ServiceResult<ForgotPasswordResponse>> ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    /// <summary>Redeems a setup or reset link: single use, invalidates other links, revokes all sessions.</summary>
    Task<ServiceResult<AdminUserResponse>> SetPasswordAsync(
        SetPasswordRequest request,
        CancellationToken cancellationToken);

    Task<ServiceResult<AuthResponse>> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken);

    /// <summary>An unknown email creates a pending account; never auto-approved.</summary>
    Task<ServiceResult<AuthResponse>> GoogleSignInAsync(
        GoogleSignInRequest request,
        CancellationToken cancellationToken);

    /// <summary>Rotates the token. A reused revoked token revokes every session for that user.</summary>
    Task<ServiceResult<AuthResponse>> RefreshAsync(
        RefreshTokenRequest request,
        CancellationToken cancellationToken);

    /// <summary>Succeeds even for unknown tokens.</summary>
    Task<ServiceResult<bool>> LogoutAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task<ServiceResult<AdminUserResponse>> GetMeAsync(CancellationToken cancellationToken);

    Task<ServiceResult<bool>> ChangePasswordAsync(
        ChangePasswordRequest request,
        CancellationToken cancellationToken);

    /// <summary>Super admin only.</summary>
    Task<ServiceResult<PagedResult<AdminUserResponse>>> GetAllAsync(
        string? search,
        UserStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    /// <summary>Super admin only. Pending accounts only; otherwise user_not_pending.</summary>
    Task<ServiceResult<AdminUserResponse>> ApproveAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Super admin only. The reason is shown to the user.</summary>
    Task<ServiceResult<AdminUserResponse>> RejectAsync(
        Guid id,
        RejectUserRequest request,
        CancellationToken cancellationToken);

    /// <summary>Super admin only. Revokes all sessions.</summary>
    Task<ServiceResult<AdminUserResponse>> DisableAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Super admin only. Soft delete; revokes all sessions.</summary>
    Task<ServiceResult<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
