using AfriPay.Application.Auth.Commands;
using AfriPay.Application.Auth.Interfaces;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Merchants;
using AfriPay.Domain.Repositories;
using NSubstitute;

namespace AfriPay.Tests.Application.Auth;

public sealed class LoginHandlerTests
{
    private readonly IUnitOfWork          _uow      = Substitute.For<IUnitOfWork>();
    private readonly IMerchantRepository  _merchants = Substitute.For<IMerchantRepository>();
    private readonly IPasswordService     _pwd      = Substitute.For<IPasswordService>();
    private readonly ITokenService        _tokens   = Substitute.For<ITokenService>();
    private readonly LoginHandler         _handler;

    public LoginHandlerTests()
    {
        _uow.Merchants.Returns(_merchants);
        _tokens.GenerateAccessToken(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns("access-token-xyz");
        _tokens.GenerateRefreshToken().Returns("refresh-token-xyz");
        _handler = new LoginHandler(_uow, _pwd, _tokens);
    }

    private static Merchant CreateActiveMerchant(string email = "shop@example.com")
    {
        var (m, _, _) = Merchant.Create("TestShop", email, "SN");
        m.SetPassword("$2a$11$hashedpassword");
        return m;
    }

    // ── Input validation ─────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_EmptyEmail_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(new LoginCommand("", "password"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task HandleAsync_EmptyPassword_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(new LoginCommand("email@test.com", ""), default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(422);
    }

    [Fact]
    public async Task HandleAsync_WhitespaceEmail_ReturnsValidationError()
    {
        var result = await _handler.HandleAsync(new LoginCommand("   ", "password"), default);

        result.IsSuccess.Should().BeFalse();
        result.Error!.StatusCode.Should().Be(422);
    }

    // ── Merchant not found ───────────────────────────────────────

    [Fact]
    public async Task HandleAsync_UnknownEmail_ReturnsGenericError()
    {
        _merchants.GetByEmailAsync("ghost@test.com", Arg.Any<CancellationToken>())
            .Returns((Merchant?)null);

        var result = await _handler.HandleAsync(new LoginCommand("ghost@test.com", "pass"), default);

        result.IsSuccess.Should().BeFalse();
        // Generic message — must not reveal whether email exists
        result.Error!.Message.Should().NotContain("not found");
    }

    [Fact]
    public async Task HandleAsync_MerchantWithNoPasswordHash_ReturnsFail()
    {
        var (m, _, _) = Merchant.Create("Shop", "shop@test.com", "CI");
        // No SetPassword call — PasswordHash is null
        _merchants.GetByEmailAsync("shop@test.com", Arg.Any<CancellationToken>()).Returns(m);

        var result = await _handler.HandleAsync(new LoginCommand("shop@test.com", "pass"), default);

        result.IsSuccess.Should().BeFalse();
    }

    // ── Wrong password ───────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_WrongPassword_ReturnsFail()
    {
        var merchant = CreateActiveMerchant("shop@test.com");
        _merchants.GetByEmailAsync("shop@test.com", Arg.Any<CancellationToken>()).Returns(merchant);
        _pwd.Verify("wrong-pass", merchant.PasswordHash!).Returns(false);

        var result = await _handler.HandleAsync(new LoginCommand("shop@test.com", "wrong-pass"), default);

        result.IsSuccess.Should().BeFalse();
    }

    // ── Suspended merchant ───────────────────────────────────────

    [Fact]
    public async Task HandleAsync_SuspendedMerchant_ReturnsFail()
    {
        var merchant = CreateActiveMerchant("shop@test.com");
        merchant.Suspend();
        _merchants.GetByEmailAsync("shop@test.com", Arg.Any<CancellationToken>()).Returns(merchant);
        _pwd.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        var result = await _handler.HandleAsync(new LoginCommand("shop@test.com", "correct-pass"), default);

        result.IsSuccess.Should().BeFalse();
        // The error references "suspended" either in Code or Message depending on the AppError.Unauthorized overload used
        (result.Error!.Code + result.Error.Message).Should().ContainAny("suspended", "Account suspended");
    }

    // ── Success ──────────────────────────────────────────────────

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsLoginDto()
    {
        var merchant = CreateActiveMerchant("shop@test.com");
        _merchants.GetByEmailAsync("shop@test.com", Arg.Any<CancellationToken>()).Returns(merchant);
        _pwd.Verify("correct-pass", merchant.PasswordHash!).Returns(true);

        var result = await _handler.HandleAsync(new LoginCommand("shop@test.com", "correct-pass"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AccessToken.Should().Be("access-token-xyz");
        result.Value.RefreshToken.Should().Be("refresh-token-xyz");
        result.Value.TokenType.Should().Be("Bearer");
        result.Value.ExpiresIn.Should().Be(900); // 15 * 60
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_NormalizesEmailToLowercase()
    {
        var merchant = CreateActiveMerchant("shop@test.com");
        _merchants.GetByEmailAsync("shop@test.com", Arg.Any<CancellationToken>()).Returns(merchant);
        _pwd.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);

        await _handler.HandleAsync(new LoginCommand("SHOP@TEST.COM", "pass"), default);

        await _merchants.Received(1)
            .GetByEmailAsync("shop@test.com", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_ValidCredentials_StoresRefreshToken()
    {
        var merchant = CreateActiveMerchant("shop@test.com");
        _merchants.GetByEmailAsync("shop@test.com", Arg.Any<CancellationToken>()).Returns(merchant);
        _pwd.Verify("pass", merchant.PasswordHash!).Returns(true);

        await _handler.HandleAsync(new LoginCommand("shop@test.com", "pass"), default);

        merchant.RefreshToken.Should().Be("refresh-token-xyz");
        merchant.RefreshTokenExpiresAt.Should().NotBeNull();
    }
}