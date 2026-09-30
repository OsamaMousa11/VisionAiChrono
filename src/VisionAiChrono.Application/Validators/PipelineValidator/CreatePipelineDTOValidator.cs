using FluentValidation;
using VisionAiChrono.Application.Dtos.Pipeline;

namespace VisionAiChrono.Application.Validators.PipelineValidator
{
    public class CreatePipelineDTOValidator : AbstractValidator<CreatePipelineDTO>
    {
        public CreatePipelineDTOValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Pipeline name is required.")
                .MaximumLength(200).WithMessage("Pipeline name cannot exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(2000).WithMessage("Description cannot exceed 2000 characters.");
        }
    }
}
