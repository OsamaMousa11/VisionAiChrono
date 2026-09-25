using FluentValidation;
using VisionAiChrono.Application.Dtos.PipelineRun;

namespace VisionAiChrono.Application.Validators.PipelineRunValidator
{
    public class CreatePipelineRunDTOValidator : AbstractValidator<CreatePipelineRunDTO>
    {
        public CreatePipelineRunDTOValidator()
        {
            RuleFor(x => x.PipelineId)
                .NotEmpty().WithMessage("Pipeline ID is required.");
        }
    }
}
