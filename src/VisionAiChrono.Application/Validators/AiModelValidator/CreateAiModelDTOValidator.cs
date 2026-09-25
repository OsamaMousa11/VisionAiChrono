using FluentValidation;
using VisionAiChrono.Application.Dtos.AiModel;

namespace VisionAiChrono.Application.Validators.AiModelValidator
{
    public class CreateAiModelDTOValidator : AbstractValidator<CreateAiModelDTO>
    {
        public CreateAiModelDTOValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("AI Model name is required.")
                .MaximumLength(200).WithMessage("AI Model name must not exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

            RuleFor(x => x.ModelType)
                .MaximumLength(200).WithMessage("Model type must not exceed 200 characters.");

            RuleFor(x => x.ConfigurationJson)
                .MaximumLength(4000).WithMessage("Configuration JSON must not exceed 4000 characters.");
        }
    }
}
