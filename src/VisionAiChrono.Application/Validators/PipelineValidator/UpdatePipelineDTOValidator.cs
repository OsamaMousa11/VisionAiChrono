using FluentValidation;
using VisionAiChrono.Application.Dtos.Pipeline;

namespace VisionAiChrono.Application.Validators.PipelineValidator
{
    public class UpdatePipelineDTOValidator : AbstractValidator<UpdatePipelineDTO>
    {
        public UpdatePipelineDTOValidator()
        {
            RuleFor(x => x.Name)
                .MaximumLength(200).WithMessage("Pipeline name must not exceed 200 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

            RuleFor(x => x)
                .Must(x => x.Name is not null || x.Description is not null)
                .WithMessage("At least one field must be provided for update.");
        }
    }
}
