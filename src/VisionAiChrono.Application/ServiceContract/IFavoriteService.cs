using VisionAiChrono.Application.Dtos.Favorite;

namespace VisionAiChrono.Application.ServiceContract
{
    public interface IFavoriteService
    {
        Task<FavoriteResponseDTO> AddAsync(AddFavoriteDTO dto, string userId);
        Task RemoveAsync(Guid pipelineId, string userId);
        Task<IEnumerable<FavoriteResponseDTO>> GetAllByUserAsync(string userId);
        Task<bool> IsFavoritedAsync(Guid pipelineId, string userId);
    }
}
