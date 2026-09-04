using FluentValidation;
using Karaakeb.Core.DTO.AuthenticationDTO;

namespace VisionAiChrono.Application.Validators.AuthenticationValidator
{
    public class ResetPasswordDTOValidator : AbstractValidator<ResetPasswordDTO>
    {
        public ResetPasswordDTOValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email address is invalid.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Verification code is required.");

            RuleFor(x => x.NewPassword)
                .NotEmpty().WithMessage("New password is required.");
        }
    }
}
