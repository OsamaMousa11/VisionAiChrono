using FluentValidation;
using Karaakeb.Core.DTO.AuthenticationDTO;

namespace VisionAiChrono.Application.Validators.AuthenticationValidator
{
    public class AddRoleDTOValidator : AbstractValidator<AddRoleDTO>
    {
        public AddRoleDTOValidator()
        {
            RuleFor(x => x.UserID)
                .NotEmpty().WithMessage("User ID is required.");

            RuleFor(x => x.RoleName)
                .NotEmpty().WithMessage("Role Name is required.");
        }
    }
}
