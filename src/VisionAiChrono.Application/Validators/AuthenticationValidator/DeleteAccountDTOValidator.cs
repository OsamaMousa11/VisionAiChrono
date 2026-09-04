using FluentValidation;
using Karaakeb.Core.DTO.AuthenticationDTO;

namespace VisionAiChrono.Application.Validators.AuthenticationValidator
{
    public class DeleteAccountDTOValidator : AbstractValidator<DeleteAccountDTO>
    {
        public DeleteAccountDTOValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("Email is required.")
                .EmailAddress().WithMessage("Email address format is invalid.");

            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("Verification code is required.");
        }
    }
}
