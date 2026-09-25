namespace VisionAiChrono.Application.Dtos.Favorite
{
    public class FavoriteResponseDTO
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid PipelineId { get; set; }
        public string PipelineName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
