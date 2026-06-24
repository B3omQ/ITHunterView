using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.User;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class UserUseCase : IUserUseCase
    {
        private readonly IUserRepository _userRepository;

        public UserUseCase(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<ResponseBase<UserMeDto>> GetMeAsync(Guid userId)
        {
            var user = await _userRepository.GetUserWithRoleAsync(userId);
            if (user == null)
            {
                throw new KeyNotFoundException("Người dùng không tồn tại.");
            }

            string fullName = user.Email;
            string? avatarUrl = null;

            if (user.CandidateProfile != null)
            {
                fullName = $"{user.CandidateProfile.FirstName} {user.CandidateProfile.LastName}".Trim();
                if (string.IsNullOrEmpty(fullName)) fullName = user.Email;
                avatarUrl = user.CandidateProfile.AvatarUrl;
            }
            else if (user.RecruiterProfile != null)
            {
                fullName = user.RecruiterProfile.FullName ?? user.Email;
                avatarUrl = user.RecruiterProfile.AvatarUrl;
            }

            var dto = new UserMeDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = fullName,
                Role = user.Role?.Name,
                AvatarUrl = avatarUrl,
                Status = user.Status.ToString(),
                CreatedAt = user.CreatedAt
            };

            return new ResponseBase<UserMeDto>(dto);
        }

        public async Task<Guid> ResolveRecruiterIdAsync(string? userIdStr)
        {
            if (Guid.TryParse(userIdStr, out var parsedId))
            {
                return parsedId;
            }

            // Fallback for development/testing: use the first seeded recruiter
            var fallbackUser = await _userRepository.GetUserByEmailAsync("recruiter1@ithunterview.com");
            if (fallbackUser != null)
            {
                return fallbackUser.Id;
            }

            return Guid.Empty;
        }
    }
}
