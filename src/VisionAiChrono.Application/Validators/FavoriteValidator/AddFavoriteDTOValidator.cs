using FluentValidation;
using VisionAiChrono.Application.Dtos.Favorite;

namespace VisionAiChrono.Application.Validators.FavoriteValidator
{
    public class AddFavoriteDTOValidator : AbstractValidator<AddFavoriteDTO>
    {
        public AddFavoriteDTOValidator()
        {
            RuleFor(x => x.PipelineId)
                .NotEmpty().WithMessage("Pipeline ID is required.");
        }
    }
}
