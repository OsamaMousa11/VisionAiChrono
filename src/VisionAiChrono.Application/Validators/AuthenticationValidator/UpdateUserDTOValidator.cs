using FluentValidation;
using Karaakeb.Core.DTO.AuthenticationDTO;

namespace VisionAiChrono.Application.Validators.AuthenticationValidator
{
    public class UpdateUserDTOValidator : AbstractValidator<UpdateUserDTO>
    {
        public UpdateUserDTOValidator()
        {
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("Email address format is invalid.")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }
}
