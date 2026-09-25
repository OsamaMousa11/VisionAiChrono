using CleanArchitectureTemplate_Application.Exceptions;
using CleanArchitectureTemplate_Domain.IRepositoryContract;
using VisionAiChrono.Application.Dtos.AiModel;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Application.Services
{
    public class AiModelService : IAiModelService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AiModelService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<AiModelResponseDTO> CreateAsync(CreateAiModelDTO dto)
        {
            var aiModel = new AiModel
            {
                Name = dto.Name,
                Description = dto.Description,
                ModelType = dto.ModelType,
                ConfigurationJson = dto.ConfigurationJson
            };

            await _unitOfWork.Repository<AiModel>().AddAsync(aiModel);
            await _unitOfWork.CompleteAsync();

            return MapToResponse(aiModel);
        }

        public async Task<AiModelResponseDTO> GetByIdAsync(Guid id)
        {
            var aiModel = await _unitOfWork.Repository<AiModel>().GetByIdAsync(id);

            if (aiModel is null)
                throw new NotFoundException($"AI Model with ID {id} not found.");

            return MapToResponse(aiModel);
        }

        public async Task<IEnumerable<AiModelResponseDTO>> GetAllAsync()
        {
            var aiModels = await _unitOfWork.Repository<AiModel>().GetAllAsync();
            return aiModels.Select(MapToResponse);
        }

        public async Task<AiModelResponseDTO> UpdateAsync(Guid id, UpdateAiModelDTO dto)
        {
            var aiModel = await _unitOfWork.Repository<AiModel>().GetByIdAsync(id);

            if (aiModel is null)
                throw new NotFoundException($"AI Model with ID {id} not found.");

            if (dto.Name is not null)
                aiModel.Name = dto.Name;

            if (dto.Description is not null)
                aiModel.Description = dto.Description;

            if (dto.ModelType is not null)
                aiModel.ModelType = dto.ModelType;

            if (dto.ConfigurationJson is not null)
                aiModel.ConfigurationJson = dto.ConfigurationJson;

            _unitOfWork.Repository<AiModel>().Update(aiModel);
            await _unitOfWork.CompleteAsync();

            return MapToResponse(aiModel);
        }

        public async Task DeleteAsync(Guid id)
        {
            var aiModel = await _unitOfWork.Repository<AiModel>().GetByIdAsync(id);

            if (aiModel is null)
                throw new NotFoundException($"AI Model with ID {id} not found.");

            _unitOfWork.Repository<AiModel>().Delete(aiModel);
            await _unitOfWork.CompleteAsync();
        }

        private static AiModelResponseDTO MapToResponse(AiModel aiModel)
        {
            return new AiModelResponseDTO
            {
                Id = aiModel.Id,
                Name = aiModel.Name,
                Description = aiModel.Description,
                ModelType = aiModel.ModelType,
                ConfigurationJson = aiModel.ConfigurationJson,
                CreatedAt = aiModel.CreatedAt,
                UpdatedAt = aiModel.UpdatedAt
            };
        }
    }
}
