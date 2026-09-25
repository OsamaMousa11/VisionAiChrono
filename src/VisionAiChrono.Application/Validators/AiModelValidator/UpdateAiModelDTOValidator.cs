using FluentValidation;
using VisionAiChrono.Application.Dtos.AiModel;

namespace VisionAiChrono.Application.Validators.AiModelValidator
{
    public class UpdateAiModelDTOValidator : AbstractValidator<UpdateAiModelDTO>
    {
        public UpdateAiModelDTOValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(200).WithMessage("AI Model name must not exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

            RuleFor(x => x.ModelType)
                .MaximumLength(200).WithMessage("Model type must not exceed 200 characters.");

            RuleFor(x => x.ConfigurationJson)
                .MaximumLength(4000).WithMessage("Configuration JSON must not exceed 4000 characters.");

            RuleFor(x => x)
                .Must(x => x.Name is not null || x.Description is not null || x.ModelType is not null || x.ConfigurationJson is not null)
                .WithMessage("At least one field must be provided for update.");
        }
    }
}
