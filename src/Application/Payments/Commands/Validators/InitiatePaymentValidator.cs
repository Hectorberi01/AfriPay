using AfriPay.Application.Payments.Dtos;
using FluentValidation;

namespace AfriPay.Application.Payments.Commands.Validators;

 
public sealed class InitiatePaymentValidator
    : AbstractValidator<InitiatePaymentDto>
{
    public InitiatePaymentValidator()
    {
        RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be positive.");
        RuleFor(x => x.Currency).Length(3).WithMessage("Currency must be ISO 4217 (3 chars).");
        RuleFor(x => x.IdempotencyKey).NotEmpty().MaximumLength(255);
        RuleFor(x => x.ProviderKey).NotEmpty().MaximumLength(30);
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^\+[1-9]\d{6,14}$")
            .When(x => x.PhoneNumber is not null)
            .WithMessage("PhoneNumber must be E.164 format (+22961234567).");
    }
}