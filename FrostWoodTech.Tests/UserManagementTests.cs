using Microsoft.EntityFrameworkCore;

using FrostWoodTech.API.Auth;
using FrostWoodTech.API.Common;
using FrostWoodTech.API.Data;
using FrostWoodTech.API.DTOs.Admin;
using FrostWoodTech.API.Entities;
using FrostWoodTech.API.Enums;
using FrostWoodTech.API.Services;

namespace FrostWoodTech.Tests;

public class UserManagementTests(PostgresFixture fixture) : DatabaseTest(fixture)
{
    private const string Password = "correct horse battery staple";

    [Fact]
    public async Task Only_the_super_admin_can_manage_users()
    {
        var target = await NewUserAsync(UserStatus.Pending);
        var actor = await NewUserAsync(UserStatus.Approved);

        await using var db = _fixture.CreateContext();
        var service = TestServices.CreateUserService(
            db, new FakeEmailService(), new CurrentUser { UserId = actor.Id, Role = UserRole.Admin });

        var approve = await service.ApproveAsync(target.Id, CancellationToken.None);
        var list = await service.GetAllAsync(null, null, 1, 20, CancellationToken.None);

        Assert.Equal(ServiceErrorKind.Forbidden, approve.Error!.Kind);
        Assert.Equal("forbidden", approve.Error.Code);
        Assert.Equal("forbidden", list.Error!.Code);
    }

    [Fact]
    public async Task Approving_a_pending_account_records_who_approved_it()
    {
        var target = await NewUserAsync(UserStatus.Pending);
        var (db, service, actor) = await SuperAdminServiceAsync();
        await using var _ = db;

        var result = await service.ApproveAsync(target.Id, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal(UserStatus.Approved, result.Value!.Status);

        await using var check = _fixture.CreateContext();
        var stored = await check.Users.SingleAsync(u => u.Id == target.Id);
        Assert.Equal(actor.Id, stored.ApprovedBy);
        Assert.NotNull(stored.ApprovedAt);
    }

    [Theory]
    [InlineData(UserStatus.Approved)]
    [InlineData(UserStatus.Rejected)]
    [InlineData(UserStatus.Disabled)]
    public async Task Only_a_pending_account_can_be_approved(UserStatus status)
    {
        var target = await NewUserAsync(status);
        var (db, service, _) = await SuperAdminServiceAsync();
        await using var __ = db;

        var result = await service.ApproveAsync(target.Id, CancellationToken.None);

        Assert.Equal("user_not_pending", result.Error!.Code);
    }

    [Fact]
    public async Task The_super_admin_cannot_manage_their_own_account()
    {
        var (db, service, actor) = await SuperAdminServiceAsync();
        await using var _ = db;

        Assert.Equal("cannot_modify_self", (await service.DisableAsync(actor.Id, CancellationToken.None)).Error!.Code);
        Assert.Equal("cannot_modify_self", (await service.DeleteAsync(actor.Id, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task The_super_admin_row_cannot_be_disabled_by_anyone()
    {
        var superAdmin = await NewUserAsync(UserStatus.Approved, UserRole.SuperAdmin, clearExistingSuperAdmin: true);
        var (db, service, _) = await SuperAdminServiceAsync();
        await using var _ = db;

        var result = await service.DisableAsync(superAdmin.Id, CancellationToken.None);

        Assert.Equal("cannot_modify_super_admin", result.Error!.Code);
    }

    [Fact]
    public async Task Managing_an_unknown_user_is_not_found()
    {
        var (db, service, _) = await SuperAdminServiceAsync();
        await using var _ = db;

        Assert.Equal("user_not_found", (await service.DisableAsync(Guid.NewGuid(), CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task Rejecting_requires_a_reason_and_the_reason_is_shown_at_login()
    {
        var target = await NewUserAsync(UserStatus.Pending);
        var (db, service, _) = await SuperAdminServiceAsync();
        await using var _ = db;

        var noReason = await service.RejectAsync(target.Id, new RejectUserRequest { Reason = "  " }, CancellationToken.None);
        Assert.Equal("validation_failed", noReason.Error!.Code);

        var rejected = await service.RejectAsync(
            target.Id, new RejectUserRequest { Reason = "Not a team member." }, CancellationToken.None);
        Assert.Equal(UserStatus.Rejected, rejected.Value!.Status);

        await using var loginDb = _fixture.CreateContext();
        var login = await TestServices.CreateUserService(loginDb, new FakeEmailService())
            .LoginAsync(new LoginRequest { Email = target.Email, Password = Password }, "127.0.0.1", CancellationToken.None);

        Assert.Equal(ServiceErrorKind.Forbidden, login.Error!.Kind);
        Assert.Equal("account_rejected", login.Error.Code);
        Assert.Equal("Not a team member.", login.Error.Message);
    }

    [Theory]
    [InlineData("disable")]
    [InlineData("reject")]
    [InlineData("delete")]
    public async Task Removing_access_revokes_every_refresh_token_and_refresh_then_fails(string action)
    {
        var target = await NewUserAsync(UserStatus.Approved);
        var refreshToken = await LoginAsync(target.Email);

        var (db, service, _) = await SuperAdminServiceAsync();
        await using (db)
        {
            var result = action switch
            {
                "disable" => (await service.DisableAsync(target.Id, CancellationToken.None)).IsSuccess,
                "reject" => (await service.RejectAsync(target.Id, new RejectUserRequest { Reason = "No." }, CancellationToken.None)).IsSuccess,
                _ => (await service.DeleteAsync(target.Id, CancellationToken.None)).IsSuccess
            };
            Assert.True(result);
        }

        await using var check = _fixture.CreateContext();
        var live = await check.RefreshTokens.CountAsync(t => t.UserId == target.Id && t.RevokedAt == null);
        Assert.Equal(0, live);

        var refresh = await TestServices.CreateUserService(check, new FakeEmailService())
            .RefreshAsync(new RefreshTokenRequest { RefreshToken = refreshToken }, CancellationToken.None);
        Assert.False(refresh.IsSuccess);
    }

    [Fact]
    public async Task A_refresh_rotates_the_token()
    {
        var target = await NewUserAsync(UserStatus.Approved);
        var original = await LoginAsync(target.Email);

        await using var db = _fixture.CreateContext();
        var refreshed = await TestServices.CreateUserService(db, new FakeEmailService())
            .RefreshAsync(new RefreshTokenRequest { RefreshToken = original }, CancellationToken.None);

        Assert.True(refreshed.IsSuccess, refreshed.Error?.Message);
        Assert.NotEqual(original, refreshed.Value!.RefreshToken);
    }

    [Fact]
    public async Task Replaying_a_rotated_token_signs_out_every_session()
    {
        var target = await NewUserAsync(UserStatus.Approved);
        var original = await LoginAsync(target.Email);

        string replacement;
        await using (var db = _fixture.CreateContext())
        {
            replacement = (await TestServices.CreateUserService(db, new FakeEmailService())
                .RefreshAsync(new RefreshTokenRequest { RefreshToken = original }, CancellationToken.None)).Value!.RefreshToken!;
        }

        await using var replayDb = _fixture.CreateContext();
        var replay = await TestServices.CreateUserService(replayDb, new FakeEmailService())
            .RefreshAsync(new RefreshTokenRequest { RefreshToken = original }, CancellationToken.None);
        Assert.Equal("refresh_token_reused", replay.Error!.Code);

        await using var afterDb = _fixture.CreateContext();
        var withReplacement = await TestServices.CreateUserService(afterDb, new FakeEmailService())
            .RefreshAsync(new RefreshTokenRequest { RefreshToken = replacement }, CancellationToken.None);
        Assert.False(withReplacement.IsSuccess);
    }

    [Fact]
    public async Task Logout_revokes_the_token_and_ignores_an_unknown_one()
    {
        var target = await NewUserAsync(UserStatus.Approved);
        var token = await LoginAsync(target.Email);

        await using var db = _fixture.CreateContext();
        var service = TestServices.CreateUserService(db, new FakeEmailService());

        Assert.True((await service.LogoutAsync(new RefreshTokenRequest { RefreshToken = "no-such-token" }, CancellationToken.None)).IsSuccess);
        Assert.True((await service.LogoutAsync(new RefreshTokenRequest { RefreshToken = token }, CancellationToken.None)).IsSuccess);

        await using var check = _fixture.CreateContext();
        Assert.Equal(0, await check.RefreshTokens.CountAsync(t => t.UserId == target.Id && t.RevokedAt == null));
    }

    [Theory]
    [InlineData(UserStatus.Pending, "account_pending")]
    [InlineData(UserStatus.Disabled, "account_disabled")]
    public async Task A_non_approved_account_gets_no_tokens(UserStatus status, string code)
    {
        var target = await NewUserAsync(status);

        await using var db = _fixture.CreateContext();
        var login = await TestServices.CreateUserService(db, new FakeEmailService())
            .LoginAsync(new LoginRequest { Email = target.Email, Password = Password }, "127.0.0.1", CancellationToken.None);

        Assert.Equal(code, login.Error!.Code);
        Assert.Equal(0, await db.RefreshTokens.CountAsync(t => t.UserId == target.Id));
    }

    [Fact]
    public async Task A_wrong_password_and_an_unknown_email_look_the_same()
    {
        var target = await NewUserAsync(UserStatus.Approved);

        await using var db = _fixture.CreateContext();
        var service = TestServices.CreateUserService(db, new FakeEmailService());

        var wrongPassword = await service.LoginAsync(
            new LoginRequest { Email = target.Email, Password = "not it" }, "127.0.0.1", CancellationToken.None);
        var unknownEmail = await service.LoginAsync(
            new LoginRequest { Email = $"nobody-{Guid.NewGuid():N}@example.com", Password = Password }, "127.0.0.1", CancellationToken.None);

        Assert.Equal(wrongPassword.Error, unknownEmail.Error);
        Assert.Equal("invalid_credentials", wrongPassword.Error!.Code);
    }

    private async Task<(FrostWoodTechDbContext Db, UserService Service, User Actor)> SuperAdminServiceAsync()
    {
        // Real row for the approved_by FK; the super admin role lives only in CurrentUser.
        var actor = await NewUserAsync(UserStatus.Approved);
        var db = _fixture.CreateContext();

        return (db, TestServices.CreateUserService(
            db, new FakeEmailService(), new CurrentUser { UserId = actor.Id, Role = UserRole.SuperAdmin }), actor);
    }

    private async Task<User> NewUserAsync(UserStatus status, UserRole role = UserRole.Admin, bool clearExistingSuperAdmin = false)
    {
        await using var db = _fixture.CreateContext();

        if (clearExistingSuperAdmin)
        {
            await db.Users.IgnoreQueryFilters().Where(u => u.Role == UserRole.SuperAdmin).ExecuteDeleteAsync();
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = $"user-{Guid.NewGuid():N}@example.com",
            FirstName = "Ada",
            LastName = "Lovelace",
            PasswordHash = PasswordHasher.Hash(Password),
            Role = role,
            Status = status,
            EmailVerifiedAt = DateTimeOffset.UtcNow
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return user;
    }

    private async Task<string> LoginAsync(string email)
    {
        await using var db = _fixture.CreateContext();

        var login = await TestServices.CreateUserService(db, new FakeEmailService())
            .LoginAsync(new LoginRequest { Email = email, Password = Password }, "127.0.0.1", CancellationToken.None);
        Assert.True(login.IsSuccess, login.Error?.Message);

        return login.Value!.RefreshToken!;
    }
}
