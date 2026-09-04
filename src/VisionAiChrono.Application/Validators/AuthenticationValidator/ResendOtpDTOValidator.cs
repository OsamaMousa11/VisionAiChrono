using FluentValidation;
using Karaakeb.Core.DTO.AuthenticationDTO;

namespace VisionAiChrono.Application.Validators.AuthenticationValidator
{
    public class ResendOtpDTOValidator : AbstractValidator<ResendOtpDTO>
    {
        public ResendOtpDTOValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email address format is invalid.");
        }
    }
}
