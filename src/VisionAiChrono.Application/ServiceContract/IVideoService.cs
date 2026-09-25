using VisionAiChrono.Application.Dtos.Video;

namespace VisionAiChrono.Application.ServiceContract
{
    public interface IVideoService
    {
        Task<VideoResponseDTO> CreateAsync(CreateVideoDTO dto);
        Task<VideoResponseDTO> GetByIdAsync(Guid id);
        Task<IEnumerable<VideoResponseDTO>> GetAllAsync();
        Task DeleteAsync(Guid id);
    }
}
