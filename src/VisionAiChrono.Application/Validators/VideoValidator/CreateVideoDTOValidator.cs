using FluentValidation;
using VisionAiChrono.Application.Dtos.Video;

namespace VisionAiChrono.Application.Validators.VideoValidator
{
    public class CreateVideoDTOValidator : AbstractValidator<CreateVideoDTO>
    {
        public CreateVideoDTOValidator()
        {
            RuleFor(x => x.FileName)
                .NotEmpty().WithMessage("File name is required.")
                .MaximumLength(500).WithMessage("File name must not exceed 500 characters.");

            RuleFor(x => x.FilePath)
                .NotEmpty().WithMessage("File path is required.")
                .MaximumLength(2000).WithMessage("File path must not exceed 2000 characters.");

            RuleFor(x => x.SizeBytes)
                .GreaterThan(0).When(x => x.SizeBytes.HasValue)
                .WithMessage("Size must be greater than 0.");

            RuleFor(x => x.ContentType)
                .MaximumLength(100).WithMessage("Content type must not exceed 100 characters.");
        }
    }
}
