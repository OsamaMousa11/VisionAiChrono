using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CleanArchitectureTemplate_Application.Exceptions;
using CleanArchitectureTemplate_Domain.IRepositoryContract;
using VisionAiChrono.Application.Dtos.Video;
using VisionAiChrono.Application.ServiceContract;
using VisionAiChrono.Domain.Model.Entity;

namespace VisionAiChrono.Application.Services
{
    public class VideoService : IVideoService
    {
        private readonly IUnitOfWork _unitOfWork;

        public VideoService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<VideoResponseDTO> CreateAsync(CreateVideoDTO dto)
        {
            var video = new Video
            {
                FileName = dto.FileName,
                FilePath = dto.FilePath,
                SizeBytes = dto.SizeBytes,
                Duration = dto.Duration,
                ContentType = dto.ContentType
            };

            await _unitOfWork.Repository<Video>().AddAsync(video);
            await _unitOfWork.CompleteAsync();

            return MapToResponse(video);
        }

        public async Task<VideoResponseDTO> GetByIdAsync(Guid id)
        {
            var video = await _unitOfWork.Repository<Video>().GetByIdAsync(id);

            if (video is null)
                throw new NotFoundException($"Video with ID {id} not found.");

            return MapToResponse(video);
        }

        public async Task<IEnumerable<VideoResponseDTO>> GetAllAsync()
        {
            var videos = await _unitOfWork.Repository<Video>().GetAllAsync();
            return videos.Select(MapToResponse);
        }

        public async Task DeleteAsync(Guid id)
        {
            var video = await _unitOfWork.Repository<Video>().GetByIdAsync(id);

            if (video is null)
                throw new NotFoundException($"Video with ID {id} not found.");

            _unitOfWork.Repository<Video>().Delete(video);
            await _unitOfWork.CompleteAsync();
        }

        private static VideoResponseDTO MapToResponse(Video video)
        {
            return new VideoResponseDTO
            {
                Id = video.Id,
                FileName = video.FileName,
                FilePath = video.FilePath,
                SizeBytes = video.SizeBytes,
                Duration = video.Duration,
                ContentType = video.ContentType,
                CreatedAt = video.CreatedAt,
                UpdatedAt = video.UpdatedAt
            };
        }
    }
}
