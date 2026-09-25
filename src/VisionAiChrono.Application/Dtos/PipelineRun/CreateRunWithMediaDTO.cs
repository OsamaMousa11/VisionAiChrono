using System.ComponentModel.DataAnnotations;

namespace VisionAiChrono.Application.Dtos.PipelineRun
{
    public class CreateRunWithMediaDTO
    {
        [Required]
        public Guid PipelineId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one model is required.")]
        public List<RunModelSelectionDTO> Models { get; set; } = new List<RunModelSelectionDTO>();
    }
}
