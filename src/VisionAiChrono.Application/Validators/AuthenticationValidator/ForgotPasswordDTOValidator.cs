using FluentValidation;
using Karaakeb.Core.DTO.AuthenticationDTO;

namespace VisionAiChrono.Application.Validators.AuthenticationValidator
{
    public class ForgotPasswordDTOValidator : AbstractValidator<ForgotPasswordDTO>
    {
        public ForgotPasswordDTOValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email address is invalid.");
        }
    }
}
