using VisionAiChrono.Application.Dtos.AiModel;

namespace VisionAiChrono.Application.ServiceContract
{
    public interface IAiModelService
    {
        Task<AiModelResponseDTO> CreateAsync(CreateAiModelDTO dto);
        Task<AiModelResponseDTO> GetByIdAsync(Guid id);
        Task<IEnumerable<AiModelResponseDTO>> GetAllAsync();
        Task<AiModelResponseDTO> UpdateAsync(Guid id, UpdateAiModelDTO dto);
        Task DeleteAsync(Guid id);
    }
}
