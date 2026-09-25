using FluentValidation;
using VisionAiChrono.Application.Dtos.PipelineRun;

namespace VisionAiChrono.Application.Validators.PipelineRunValidator
{
    public class AddPipelineRunVideoDTOValidator : AbstractValidator<AddPipelineRunVideoDTO>
    {
        public AddPipelineRunVideoDTOValidator()
        {
            RuleFor(x => x.VideoId)
                .NotEmpty().WithMessage("Video ID is required.");
        }
    }
}
