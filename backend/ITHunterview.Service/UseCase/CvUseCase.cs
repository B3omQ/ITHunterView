using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CvUseCase : ICvUseCase
    {
        private readonly ICvRepository _cvRepository;

        public CvUseCase(ICvRepository cvRepository)
        {
            _cvRepository = cvRepository;
        }

        public async Task<CvResponseDto> CreateCvAsync(Guid userId, CreateCvRequestDto request)
        {
            if (request.IsPrimary)
            {
                await _cvRepository.ResetPrimaryCvAsync(userId);
            }
            else
            {
                bool hasPrimary = await _cvRepository.HasPrimaryCvAsync(userId);
                if (!hasPrimary)
                {
                    request.IsPrimary = true;
                }
            }

            var cv = new Cvs
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FileUrl = request.FileUrl,
                FileName = request.FileName,
                FileSize = request.FileSize,
                FileType = request.FileType,
                IsPrimary = request.IsPrimary,
                ParsedData = request.ParsedData,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var createdCv = await _cvRepository.CreateAsync(cv);

            return MapToDto(createdCv);
        }

        public async Task<IEnumerable<CvResponseDto>> GetMyCvsAsync(Guid userId)
        {
            var cvs = await _cvRepository.GetByUserIdAsync(userId);
            return cvs.Select(MapToDto);
        }

        public async Task<CvResponseDto> GetCvByIdAsync(Guid id, Guid userId)
        {
            var cv = await _cvRepository.GetByIdAsync(id);
            if (cv == null || cv.UserId != userId)
            {
                throw new KeyNotFoundException("CV not found");
            }

            return MapToDto(cv);
        }

        public async Task DeleteCvAsync(Guid id, Guid userId)
        {
            var cv = await _cvRepository.GetByIdAsync(id);
            if (cv == null || cv.UserId != userId)
            {
                throw new KeyNotFoundException("CV not found");
            }

            await _cvRepository.DeleteAsync(cv);

            if (cv.IsPrimary)
            {
                var remainingCvs = await _cvRepository.GetByUserIdAsync(userId);
                var newestCv = remainingCvs.FirstOrDefault();
                if (newestCv != null)
                {
                    newestCv.IsPrimary = true;
                    newestCv.UpdatedAt = DateTime.UtcNow;
                    await _cvRepository.UpdateAsync(newestCv);
                }
            }
        }

        private CvResponseDto MapToDto(Cvs cv)
        {
            return new CvResponseDto
            {
                Id = cv.Id,
                UserId = cv.UserId,
                FileUrl = cv.FileUrl,
                FileName = cv.FileName,
                FileSize = cv.FileSize,
                FileType = cv.FileType,
                IsPrimary = cv.IsPrimary,
                ParsedData = cv.ParsedData,
                CreatedAt = cv.CreatedAt,
                UpdatedAt = cv.UpdatedAt
            };
        }
    }
}
