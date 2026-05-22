using AfriPay.Application.Payments.Commands.Validators;
using AfriPay.Application.Payments.Dtos;
using AfriPay.Application.Common.Errors;

namespace AfriPay.Tests.Application.Payments;

public sealed class InitiatePaymentValidatorTests
{
    private readonly InitiatePaymentValidator _validator = new();

    private static InitiatePaymentDto ValidDto() => new(
        MerchantId:     Guid.NewGuid(),
        IdempotencyKey: "idem-001",
        Amount:         5000,
        Currency:       "XOF",
        ProviderKey:    "mtn_momo",
        PhoneNumber:    "+22961234567",
        Email:          null,
        Metadata:       null,
        WebhookUrl:     null,
        IsLive:         false);

    // ── Happy path ───────────────────────────────────────────────

    [Fact]
    public void Validate_ValidDto_Passes()
    {
        var result = _validator.Validate(ValidDto());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_NullPhoneNumber_Passes()
    {
        var result = _validator.Validate(ValidDto() with { PhoneNumber = null });

        result.IsValid.Should().BeTrue();
    }

    // ── Amount ───────────────────────────────────────────────────

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10000)]
    public void Validate_AmountNotPositive_FailsOnAmount(long amount)
    {
        var result = _validator.Validate(ValidDto() with { Amount = amount });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Amount");
    }

    // ── Currency ─────────────────────────────────────────────────

    [Theory]
    [InlineData("XO")]
    [InlineData("XOFO")]
    [InlineData("")]
    public void Validate_InvalidCurrencyLength_FailsOnCurrency(string currency)
    {
        var result = _validator.Validate(ValidDto() with { Currency = currency });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Currency");
    }

    // ── IdempotencyKey ───────────────────────────────────────────

    [Fact]
    public void Validate_EmptyIdempotencyKey_Fails()
    {
        var result = _validator.Validate(ValidDto() with { IdempotencyKey = "" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdempotencyKey");
    }

    [Fact]
    public void Validate_IdempotencyKeyTooLong_Fails()
    {
        var result = _validator.Validate(ValidDto() with { IdempotencyKey = new string('a', 256) });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "IdempotencyKey");
    }

    // ── ProviderKey ──────────────────────────────────────────────

    [Fact]
    public void Validate_EmptyProviderKey_Fails()
    {
        var result = _validator.Validate(ValidDto() with { ProviderKey = "" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProviderKey");
    }

    // ── PhoneNumber ──────────────────────────────────────────────

    [Theory]
    [InlineData("22961234567")]     // missing +
    [InlineData("+229")]            // too short (< 7 digits)
    [InlineData("+abcdefghijk")]    // non-digits
    [InlineData("0022961234567")]   // leading 00 instead of +
    public void Validate_InvalidPhoneNumber_FailsOnPhone(string phone)
    {
        var result = _validator.Validate(ValidDto() with { PhoneNumber = phone });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PhoneNumber");
    }

    [Theory]
    [InlineData("+22961234567")]
    [InlineData("+33612345678")]
    [InlineData("+12025551234")]
    public void Validate_ValidPhoneNumber_Passes(string phone)
    {
        var result = _validator.Validate(ValidDto() with { PhoneNumber = phone });

        result.IsValid.Should().BeTrue();
    }
}