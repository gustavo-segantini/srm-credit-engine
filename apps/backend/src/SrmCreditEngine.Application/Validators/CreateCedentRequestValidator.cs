using FluentValidation;
using SrmCreditEngine.Application.DTOs.Requests;

namespace SrmCreditEngine.Application.Validators;

public sealed class CreateCedentRequestValidator : AbstractValidator<CreateCedentRequest>
{
    public CreateCedentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.Cnpj)
            .NotEmpty().WithMessage("CNPJ is required.")
            .Length(14).WithMessage("CNPJ must contain exactly 14 digits.")
            .Matches(@"^\d{14}$").WithMessage("CNPJ must contain only digits.");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("ContactEmail must be a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));
    }
}
