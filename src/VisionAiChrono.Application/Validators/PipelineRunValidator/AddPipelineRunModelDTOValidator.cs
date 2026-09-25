using FluentValidation;
using VisionAiChrono.Application.Dtos.PipelineRun;

namespace VisionAiChrono.Application.Validators.PipelineRunValidator
{
    public class AddPipelineRunModelDTOValidator : AbstractValidator<AddPipelineRunModelDTO>
    {
        public AddPipelineRunModelDTOValidator()
        {
            RuleFor(x => x.AiModelId)
                .NotEmpty().WithMessage("AI Model ID is required.");

            RuleFor(x => x.Order)
                .GreaterThanOrEqualTo(0).WithMessage("Order must be greater than or equal to 0.");

            RuleFor(x => x.ConfigurationJson)
                .MaximumLength(4000).WithMessage("Configuration JSON must not exceed 4000 characters.");
        }
    }
}
