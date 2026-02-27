using FluentValidation;
using SrmCreditEngine.Application.DTOs.Requests;

namespace SrmCreditEngine.Application.Validators;

public sealed class UpdateCedentRequestValidator : AbstractValidator<UpdateCedentRequest>
{
    public UpdateCedentRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200).WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.ContactEmail)
            .EmailAddress().WithMessage("ContactEmail must be a valid email address.")
            .When(x => !string.IsNullOrEmpty(x.ContactEmail));
    }
}
