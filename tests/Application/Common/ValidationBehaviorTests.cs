using AfriPay.Application.Common.Behaviors;
using FluentValidation;
using FluentValidation.Results;
using MediatR;
using NSubstitute;

namespace AfriPay.Tests.Application.Common;

// Must be top-level public — NSubstitute cannot proxy IValidator<T> when T is a private nested type
// (FluentValidation is strong-named, so Castle.DynamicProxy requires T to be accessible from DynamicProxyGenAssembly2)
public sealed record ValidationTestRequest : IRequest<string>;

public sealed class ValidationBehaviorTests
{
    // ── No validators ────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoValidators_CallsNextAndReturnsResult()
    {
        var behavior = new ValidationBehavior<ValidationTestRequest, string>([]);
        var called   = false;
        RequestHandlerDelegate<string> next = _ => { called = true; return Task.FromResult("ok"); };

        var result = await behavior.Handle(new ValidationTestRequest(), next, default);

        result.Should().Be("ok");
        called.Should().BeTrue();
    }

    // ── Passing validator ────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidatorPasses_CallsNext()
    {
        var validator = Substitute.For<IValidator<ValidationTestRequest>>();
        validator.Validate(Arg.Any<ValidationContext<ValidationTestRequest>>())
            .Returns(new ValidationResult());

        var behavior = new ValidationBehavior<ValidationTestRequest, string>([validator]);
        var called   = false;
        RequestHandlerDelegate<string> next = _ => { called = true; return Task.FromResult("ok"); };

        var result = await behavior.Handle(new ValidationTestRequest(), next, default);

        result.Should().Be("ok");
        called.Should().BeTrue();
    }

    // ── Failing validator ────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidatorFails_ThrowsValidationExceptionAndDoesNotCallNext()
    {
        var failure   = new ValidationFailure("Amount", "Amount must be positive.");
        var validator = Substitute.For<IValidator<ValidationTestRequest>>();
        validator.Validate(Arg.Any<ValidationContext<ValidationTestRequest>>())
            .Returns(new ValidationResult([failure]));

        var behavior = new ValidationBehavior<ValidationTestRequest, string>([validator]);
        var called   = false;
        RequestHandlerDelegate<string> next = _ => { called = true; return Task.FromResult("ok"); };

        var act = async () => await behavior.Handle(new ValidationTestRequest(), next, default);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Any(e => e.PropertyName == "Amount"));
        called.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_MultipleFailures_AllErrorsIncludedInException()
    {
        var failures = new[]
        {
            new ValidationFailure("Amount",   "Required."),
            new ValidationFailure("Currency", "Must be 3 chars."),
        };
        var validator = Substitute.For<IValidator<ValidationTestRequest>>();
        validator.Validate(Arg.Any<ValidationContext<ValidationTestRequest>>())
            .Returns(new ValidationResult(failures));

        var behavior = new ValidationBehavior<ValidationTestRequest, string>([validator]);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var act = async () => await behavior.Handle(new ValidationTestRequest(), next, default);

        await act.Should().ThrowAsync<ValidationException>()
            .Where(ex => ex.Errors.Count() == 2);
    }

    // ── Multiple validators ──────────────────────────────────────

    [Fact]
    public async Task Handle_MultipleValidatorsOneFails_ThrowsValidationException()
    {
        var passing = Substitute.For<IValidator<ValidationTestRequest>>();
        passing.Validate(Arg.Any<ValidationContext<ValidationTestRequest>>())
            .Returns(new ValidationResult());

        var failing = Substitute.For<IValidator<ValidationTestRequest>>();
        failing.Validate(Arg.Any<ValidationContext<ValidationTestRequest>>())
            .Returns(new ValidationResult([new ValidationFailure("X", "Required.")]));

        var behavior = new ValidationBehavior<ValidationTestRequest, string>([passing, failing]);
        RequestHandlerDelegate<string> next = _ => Task.FromResult("ok");

        var act = async () => await behavior.Handle(new ValidationTestRequest(), next, default);

        await act.Should().ThrowAsync<ValidationException>();
    }
}