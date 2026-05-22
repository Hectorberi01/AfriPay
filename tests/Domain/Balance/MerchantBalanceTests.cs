using AfriPay.Domain.Balance;
using AfriPay.Domain.Exceptions;

namespace AfriPay.Tests.Domain.Balance;

public sealed class MerchantBalanceTests
{
    private static readonly Guid MerchantId = Guid.NewGuid();

    private static MerchantBalance CreateBalance(string currency = "XOF") =>
        MerchantBalance.Create(MerchantId, currency);

    // Create 

    [Fact]
    public void Create_InitializesWithZeroBalancesAndActiveStatus()
    {
        var balance = CreateBalance();

        balance.AvailableBalance.Should().Be(0);
        balance.PendingBalance.Should().Be(0);
        balance.ReservedBalance.Should().Be(0);
        balance.Status.Should().Be(BalanceStatus.Active);
        balance.MerchantId.Should().Be(MerchantId);
    }

    [Fact]
    public void Create_NormalizesCurrencyToUpperCase()
    {
        var balance = CreateBalance("xof");

        balance.Currency.Should().Be("XOF");
    }

    // CreditPending 

    [Fact]
    public void CreditPending_IncreasesPendingBalance()
    {
        var balance = CreateBalance();

        balance.CreditPending(5_000, "payment-1");

        balance.PendingBalance.Should().Be(5_000);
        balance.AvailableBalance.Should().Be(0);
    }

    [Fact]
    public void CreditPending_ReturnsBalanceEntryWithCorrectAmount()
    {
        var balance = CreateBalance();

        var entry = balance.CreditPending(5_000, "payment-1", "Test credit");

        entry.Amount.Should().Be(5_000);
        entry.Currency.Should().Be("XOF");
    }

    [Fact]
    public void CreditPending_FrozenBalance_ThrowsDomainException()
    {
        var balance = CreateBalance();
        balance.Freeze();

        var act = () => balance.CreditPending(5_000, "payment-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*frozen*");
    }

    //Settle

    [Fact]
    public void Settle_TransfersPendingToAvailable()
    {
        var balance = CreateBalance();
        balance.CreditPending(5_000, "payment-1");

        balance.Settle(5_000, "settlement-1");

        balance.PendingBalance.Should().Be(0);
        balance.AvailableBalance.Should().Be(5_000);
    }

    [Fact]
    public void Settle_PartialAmount_OnlyTransfersSpecifiedAmount()
    {
        var balance = CreateBalance();
        balance.CreditPending(10_000, "payment-1");

        balance.Settle(6_000, "settlement-1");

        balance.PendingBalance.Should().Be(4_000);
        balance.AvailableBalance.Should().Be(6_000);
    }

    [Fact]
    public void Settle_MoreThanPending_ThrowsDomainException()
    {
        var balance = CreateBalance();
        balance.CreditPending(3_000, "payment-1");

        var act = () => balance.Settle(5_000, "settlement-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*pending balance*");
    }

    //DebitForRefund

    [Fact]
    public void DebitForRefund_MovesAvailableToReserved()
    {
        var balance = CreateBalance();
        balance.CreditPending(10_000, "p-1");
        balance.Settle(10_000, "s-1");

        balance.DebitForRefund(3_000, "refund-1");

        balance.AvailableBalance.Should().Be(7_000);
        balance.ReservedBalance.Should().Be(3_000);
    }

    [Fact]
    public void DebitForRefund_InsufficientAvailable_ThrowsDomainException()
    {
        var balance = CreateBalance();

        var act = () => balance.DebitForRefund(1_000, "refund-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*Insufficient balance for refund*");
    }

    // DebitFee 

    [Fact]
    public void DebitFee_DeductsFromAvailable()
    {
        var balance = CreateBalance();
        balance.CreditPending(10_000, "p-1");
        balance.Settle(10_000, "s-1");

        balance.DebitFee(200, "payment-1");

        balance.AvailableBalance.Should().Be(9_800);
    }

    [Fact]
    public void DebitFee_InsufficientAvailable_ThrowsDomainException()
    {
        var balance = CreateBalance();

        var act = () => balance.DebitFee(100, "payment-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*Insufficient balance for fee*");
    }

    // DebitForPayout 

    [Fact]
    public void DebitForPayout_DeductsFromAvailable()
    {
        var balance = CreateBalance();
        balance.CreditPending(20_000, "p-1");
        balance.Settle(20_000, "s-1");

        balance.DebitForPayout(15_000, "payout-1");

        balance.AvailableBalance.Should().Be(5_000);
    }

    [Fact]
    public void DebitForPayout_InsufficientAvailable_ThrowsDomainException()
    {
        var balance = CreateBalance();

        var act = () => balance.DebitForPayout(1_000, "payout-1");

        act.Should().Throw<DomainException>()
            .WithMessage("*Insufficient available balance for payout*");
    }

    // ReleaseReserve

    [Fact]
    public void ReleaseReserve_DeductsFromReserved()
    {
        var balance = CreateBalance();
        balance.CreditPending(10_000, "p-1");
        balance.Settle(10_000, "s-1");
        balance.DebitForRefund(5_000, "refund-1");

        balance.ReleaseReserve(5_000);

        balance.ReservedBalance.Should().Be(0);
    }

    [Fact]
    public void ReleaseReserve_MoreThanReserved_ThrowsDomainException()
    {
        var balance = CreateBalance();

        var act = () => balance.ReleaseReserve(1_000);

        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot release more than reserved*");
    }

    //Freeze / Unfreeze

    [Fact]
    public void Freeze_ActiveBalance_SetsFrozenStatus()
    {
        var balance = CreateBalance();

        balance.Freeze();

        balance.Status.Should().Be(BalanceStatus.Frozen);
    }

    [Fact]
    public void Freeze_AlreadyFrozen_ThrowsDomainException()
    {
        var balance = CreateBalance();
        balance.Freeze();

        var act = () => balance.Freeze();

        act.Should().Throw<DomainException>()
            .WithMessage("*already frozen*");
    }

    [Fact]
    public void Unfreeze_FrozenBalance_SetsActiveStatus()
    {
        var balance = CreateBalance();
        balance.Freeze();

        balance.Unfreeze();

        balance.Status.Should().Be(BalanceStatus.Active);
    }

    [Fact]
    public void Unfreeze_NotFrozen_ThrowsDomainException()
    {
        var balance = CreateBalance();

        var act = () => balance.Unfreeze();

        act.Should().Throw<DomainException>()
            .WithMessage("*not frozen*");
    }

    // Computed 

    [Fact]
    public void TotalBalance_EqualsAvailablePlusPending()
    {
        var balance = CreateBalance();
        balance.CreditPending(10_000, "p-1");
        balance.Settle(4_000, "s-1");

        balance.TotalBalance.Should().Be(10_000);
    }

    [Fact]
    public void HasSufficientFunds_EnoughAvailable_ReturnsTrue()
    {
        var balance = CreateBalance();
        balance.CreditPending(10_000, "p-1");
        balance.Settle(10_000, "s-1");

        balance.HasSufficientFunds(5_000).Should().BeTrue();
    }

    [Fact]
    public void HasSufficientFunds_NotEnoughAvailable_ReturnsFalse()
    {
        var balance = CreateBalance();

        balance.HasSufficientFunds(1).Should().BeFalse();
    }
}