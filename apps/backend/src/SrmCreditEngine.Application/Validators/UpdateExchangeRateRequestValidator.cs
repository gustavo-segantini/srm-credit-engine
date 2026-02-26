using FluentValidation;
using SrmCreditEngine.Application.DTOs.Requests;

namespace SrmCreditEngine.Application.Validators;

public sealed class UpdateExchangeRateRequestValidator : AbstractValidator<UpdateExchangeRateRequest>
{
    public UpdateExchangeRateRequestValidator()
    {
        RuleFor(x => x.FromCurrency)
            .IsInEnum().WithMessage("Invalid source currency.");

        RuleFor(x => x.ToCurrency)
            .IsInEnum().WithMessage("Invalid target currency.");

        RuleFor(x => x)
            .Must(x => x.FromCurrency != x.ToCurrency)
            .WithMessage("Source and target currencies must be different.");

        RuleFor(x => x.Rate)
            .GreaterThan(0).WithMessage("Exchange rate must be greater than zero.")
            .LessThanOrEqualTo(999_999m).WithMessage("Exchange rate seems unrealistic.");

        RuleFor(x => x.Source)
            .NotEmpty().WithMessage("Rate source is required.")
            .MaximumLength(100).WithMessage("Source must not exceed 100 characters.");
    }
}
