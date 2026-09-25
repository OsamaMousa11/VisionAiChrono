using CleanArchitectureTemplate_Application.Exceptions;
using CleanArchitectureTemplate_Domain.IRepositoryContract;
using VisionAiChrono.Application.Dtos.Favorite;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Application.Services
{
    public class FavoriteService : IFavoriteService
    {
        private readonly IUnitOfWork _unitOfWork;

        public FavoriteService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<FavoriteResponseDTO> AddAsync(AddFavoriteDTO dto, string userId)
        {
            var pipeline = await _unitOfWork.Repository<Pipeline>().GetByIdAsync(dto.PipelineId);

            if (pipeline is null)
                throw new NotFoundException($"Pipeline with ID {dto.PipelineId} not found.");

            var exists = await _unitOfWork.Repository<Favorite>()
                .ExistsAsync(f => f.UserId == Guid.Parse(userId) && f.PipelineId == dto.PipelineId);

            if (exists)
                throw new BadRequestException("This pipeline is already in your favorites.");

            var favorite = new Favorite
            {
                UserId = Guid.Parse(userId),
                PipelineId = dto.PipelineId
            };

            await _unitOfWork.Repository<Favorite>().AddAsync(favorite);
            await _unitOfWork.CompleteAsync();

            return new FavoriteResponseDTO
            {
                Id = favorite.Id,
                UserId = favorite.UserId,
                PipelineId = favorite.PipelineId,
                PipelineName = pipeline.Name,
                CreatedAt = favorite.CreatedAt
            };
        }

        public async Task RemoveAsync(Guid pipelineId, string userId)
        {
            var favorite = await _unitOfWork.Repository<Favorite>()
                .FindAsync(f => f.UserId == Guid.Parse(userId) && f.PipelineId == pipelineId);

            if (favorite is null)
                throw new NotFoundException($"Favorite not found for Pipeline {pipelineId}.");

            _unitOfWork.Repository<Favorite>().Delete(favorite);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<IEnumerable<FavoriteResponseDTO>> GetAllByUserAsync(string userId)
        {
            var favorites = await _unitOfWork.Repository<Favorite>()
                .FindAllAsync(f => f.UserId == Guid.Parse(userId), "Pipeline");

            return favorites.Select(f => new FavoriteResponseDTO
            {
                Id = f.Id,
                UserId = f.UserId,
                PipelineId = f.PipelineId,
                PipelineName = f.Pipeline?.Name ?? string.Empty,
                CreatedAt = f.CreatedAt
            });
        }

        public async Task<bool> IsFavoritedAsync(Guid pipelineId, string userId)
        {
            return await _unitOfWork.Repository<Favorite>()
                .ExistsAsync(f => f.UserId == Guid.Parse(userId) && f.PipelineId == pipelineId);
        }
    }
}
