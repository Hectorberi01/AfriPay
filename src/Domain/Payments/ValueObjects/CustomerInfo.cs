using AfriPay.Domain.Exceptions;

namespace AfriPay.Domain.Payments.ValueObjects;

public sealed class CustomerInfo
{
    public string? PhoneNumber { get; init; }   // E.164 : +22961234567
    public string? Email       { get; init; }
    public string? Name        { get; init; }
 
    public CustomerInfo() { }
 
    public CustomerInfo(string? phoneNumber, string? email, string? name)
    {
        if (phoneNumber is not null && !phoneNumber.StartsWith('+'))
            throw new DomainException("PhoneNumber must be in E.164 format (+XXXXXXXXXXX).");
 
        PhoneNumber = phoneNumber;
        Email       = email;
        Name        = name;
    }
}