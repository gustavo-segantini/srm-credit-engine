using FluentValidation;
using SrmCreditEngine.Application.DTOs.Requests;

namespace SrmCreditEngine.Application.Validators;

public sealed class SimulatePricingRequestValidator : AbstractValidator<SimulatePricingRequest>
{
    public SimulatePricingRequestValidator()
    {
        RuleFor(x => x.FaceValue)
            .GreaterThan(0).WithMessage("Face value must be greater than zero.")
            .LessThanOrEqualTo(999_999_999.99m).WithMessage("Face value exceeds maximum allowed.");

        RuleFor(x => x.DueDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Due date must be in the future.");

        RuleFor(x => x.ReceivableType)
            .IsInEnum().WithMessage("Invalid receivable type.");

        RuleFor(x => x.FaceCurrency)
            .IsInEnum().WithMessage("Invalid face currency.");

        RuleFor(x => x.PaymentCurrency)
            .IsInEnum().WithMessage("Invalid payment currency.");
    }
}
