using FluentValidation;
using Karaakeb.Core.DTO.AuthenticationDTO;

namespace VisionAiChrono.Application.Validators.AuthenticationValidator
{
    public class LoginDTOValidator : AbstractValidator<LoginDTO>
    {
        public LoginDTOValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email address is invalid.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Password is required.");
        }
    }
}
