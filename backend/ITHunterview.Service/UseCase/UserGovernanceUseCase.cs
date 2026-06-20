using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.UserGovernance;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.Extensions.Caching.Memory;

namespace ITHunterview.Service.UseCase
{
    public class UserGovernanceUseCase : IUserGovernanceUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IMemoryCache _cache;

        public UserGovernanceUseCase(IUserRepository userRepository, ITokenRepository tokenRepository, IMemoryCache cache)
        {
            _userRepository = userRepository;
            _tokenRepository = tokenRepository;
            _cache = cache;
            _auditLogRepository = auditLogRepository;
        }

        public async Task<ResponseBase<PagedResult<UserDto>>> GetPagedUsersAsync(int page, int pageSize, string? search, int? roleId, UserStatus? status)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var (items, total) = await _userRepository.GetPagedUsersAsync(page, pageSize, search, roleId, status);

            var dtos = items.Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email,
                RoleId = u.RoleId,
                RoleName = u.RoleId == (int)SystemRole.Admin ? "admin" :
                           u.RoleId == (int)SystemRole.Staff ? "staff" :
                           u.RoleId == (int)SystemRole.Recruiter ? "recruiter" :
                           u.RoleId == (int)SystemRole.Candidate ? "candidate" : string.Empty,
                Status = u.Status,
                FullName = u.RoleId == (int)SystemRole.Recruiter 
                    ? (u.RecruiterProfile?.FullName ?? string.Empty) 
                    : ($"{u.CandidateProfile?.FirstName} {u.CandidateProfile?.LastName}").Trim(),
                Phone = u.RoleId == (int)SystemRole.Recruiter 
                    ? u.RecruiterProfile?.Phone 
                    : u.CandidateProfile?.Phone,
                CreatedAt = u.CreatedAt,
                DeactiveAt = u.DeactiveAt
            }).ToList();

            var result = new PagedResult<UserDto>
            {
                Items = dtos,
                Total = total,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<UserDto>>(result);
        }

        public async Task<ResponseBase<UserDetailDto>> GetUserDetailAsync(Guid userId)
        {
            var user = await _userRepository.GetUserDetailWithCompanyAsync(userId);
            if (user == null)
            {
                return new ResponseBase<UserDetailDto>("Người dùng không tồn tại.");
            }

            var dto = new UserDetailDto
            {
                Id = user.Id,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.RoleId == (int)SystemRole.Admin ? "admin" :
                           user.RoleId == (int)SystemRole.Staff ? "staff" :
                           user.RoleId == (int)SystemRole.Recruiter ? "recruiter" :
                           user.RoleId == (int)SystemRole.Candidate ? "candidate" : string.Empty,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                DeactiveAt = user.DeactiveAt
            };

            if (user.CandidateProfile != null)
            {
                dto.CandidateProfile = new CandidateProfileDetailDto
                {
                    FirstName = user.CandidateProfile.FirstName ?? string.Empty,
                    LastName = user.CandidateProfile.LastName ?? string.Empty,
                    Phone = user.CandidateProfile.Phone ?? string.Empty,
                    Location = user.CandidateProfile.Location ?? string.Empty,
                    AboutMe = user.CandidateProfile.AboutMe ?? string.Empty,
                    AvatarUrl = user.CandidateProfile.AvatarUrl ?? string.Empty,
                    GithubUrl = user.CandidateProfile.GithubUrl ?? string.Empty,
                    LinkedInUrl = user.CandidateProfile.LinkedinUrl ?? string.Empty,
                    PortfolioUrl = user.CandidateProfile.PortfolioUrl ?? string.Empty
                };
            }

            if (user.RecruiterProfile != null)
            {
                dto.RecruiterProfile = new RecruiterProfileDetailDto
                {
                    FullName = user.RecruiterProfile.FullName ?? string.Empty,
                    Phone = user.RecruiterProfile.Phone ?? string.Empty,
                    PositionTitle = user.RecruiterProfile.PositionTitle ?? string.Empty,
                    AvatarUrl = user.RecruiterProfile.AvatarUrl ?? string.Empty
                };

                if (user.RecruiterProfile.Company != null)
                {
                    dto.RecruiterProfile.Company = new CompanyDetailDto
                    {
                        Id = user.RecruiterProfile.Company.Id,
                        Name = user.RecruiterProfile.Company.Name ?? string.Empty,
                        HeadquartersAddress = user.RecruiterProfile.Company.HeadquartersAddress ?? string.Empty,
                        Industry = user.RecruiterProfile.Company.Industry ?? string.Empty,
                        Website = user.RecruiterProfile.Company.Website ?? string.Empty,
                        LogoUrl = user.RecruiterProfile.Company.LogoUrl ?? string.Empty
                    };
                }
            }

            return new ResponseBase<UserDetailDto>(dto);
        }

        public async Task<ResponseBase<bool>> UpdateUserStatusAsync(
            Guid targetUserId, 
            UpdateUserStatusDto dto, 
            Guid actorUserId, 
            string actorEmail, 
            string actorRole, 
            string ipAddress, 
            string userAgent)
        {
            // 1. Prevent admin self lockout
            if (targetUserId == actorUserId)
            {
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    ActivityLogCategory.SECURITY, 
                    actorEmail, 
                    "Thất bại: Quản trị viên tự thay đổi trạng thái hoạt động của chính mình.", 
                    ActivityLogStatus.FAIL, 
                    ipAddress, 
                    userAgent);
                return new ResponseBase<bool>("Bạn không thể tự cập nhật trạng thái hoạt động của chính mình.");
            }

            // 2. Fetch target user
            var targetUser = await _userRepository.GetUserByIdAsync(targetUserId);
            if (targetUser == null)
            {
                return new ResponseBase<bool>("Người dùng không tồn tại.");
            }

            var oldStatus = targetUser.Status;
            
            try
            {
                // 3. Update status
                targetUser.Status = dto.Status;
                if (dto.Status == UserStatus.INACTIVE || dto.Status == UserStatus.BANNED)
                {
                    targetUser.DeactiveAt = DateTime.UtcNow;
                }
                else
                {
                    targetUser.DeactiveAt = null;
                }
                targetUser.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateUserAsync(targetUser);
                _cache.Remove($"user-status-{targetUserId}");

                // 4. Revoke active sessions if BANNED or INACTIVE
                if (dto.Status == UserStatus.INACTIVE || dto.Status == UserStatus.BANNED)
                {
                    await _tokenRepository.RevokeAllUserRefreshTokensAsync(targetUserId);
                }

                // 5. Log success
                string logAction = $"Cập nhật trạng thái người dùng {targetUser.Email} từ {oldStatus} thành {dto.Status}. Lý do: {dto.Reason}";
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    dto.Status == UserStatus.BANNED ? ActivityLogCategory.SECURITY : ActivityLogCategory.DATA_MUTATION, 
                    actorEmail, 
                    logAction, 
                    ActivityLogStatus.SUCCESS, 
                    ipAddress, 
                    userAgent);

                return new ResponseBase<bool>(true, $"Cập nhật trạng thái hoạt động thành {dto.Status} thành công.");
            }
            catch (Exception ex)
            {
                string logAction = $"Lỗi cập nhật trạng thái người dùng {targetUser.Email} từ {oldStatus} sang {dto.Status}: {ex.Message}";
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    ActivityLogCategory.SYSTEM, 
                    actorEmail, 
                    logAction, 
                    ActivityLogStatus.FAIL, 
                    ipAddress, 
                    userAgent);

                return new ResponseBase<bool>($"Lỗi hệ thống khi cập nhật trạng thái: {ex.Message}");
            }
        }

        public async Task<ResponseBase<bool>> UpdateUserRoleAsync(
            Guid targetUserId, 
            UpdateUserRoleDto dto, 
            Guid actorUserId, 
            string actorEmail, 
            string actorRole, 
            string ipAddress, 
            string userAgent)
        {
            // 1. Staff is not allowed to change roles (only admin can change role)
            if (actorRole.ToLower() != "admin")
            {
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    ActivityLogCategory.SECURITY, 
                    actorEmail, 
                    "Thất bại: Nhân viên (Staff) cố gắng thay đổi vai trò người dùng.", 
                    ActivityLogStatus.FAIL, 
                    ipAddress, 
                    userAgent);
                return new ResponseBase<bool>("Chỉ quản trị viên cấp cao (Admin) mới có quyền thay đổi vai trò người dùng.");
            }

            // 2. Prevent admin self role mutation
            if (targetUserId == actorUserId)
            {
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    ActivityLogCategory.SECURITY, 
                    actorEmail, 
                    "Thất bại: Quản trị viên tự thay đổi vai trò của chính mình.", 
                    ActivityLogStatus.FAIL, 
                    ipAddress, 
                    userAgent);
                return new ResponseBase<bool>("Bạn không thể tự cập nhật vai trò của chính mình.");
            }

            // 3. Fetch target user
            var targetUser = await _userRepository.GetUserWithRoleAsync(targetUserId);
            if (targetUser == null)
            {
                return new ResponseBase<bool>("Người dùng không tồn tại.");
            }

            // 4. Validate new role existence
            var roleExists = await _userRepository.RoleExistsAsync(dto.RoleId);
            if (!roleExists)
            {
                return new ResponseBase<bool>("Vai trò được chọn không tồn tại trên hệ thống.");
            }

            var oldRoleName = targetUser.RoleId == (int)SystemRole.Admin ? "admin" :
                              targetUser.RoleId == (int)SystemRole.Staff ? "staff" :
                              targetUser.RoleId == (int)SystemRole.Recruiter ? "recruiter" :
                              targetUser.RoleId == (int)SystemRole.Candidate ? "candidate" : "Không xác định";
            var newRoleName = dto.RoleId == (int)SystemRole.Admin ? "admin" :
                              dto.RoleId == (int)SystemRole.Staff ? "staff" :
                              dto.RoleId == (int)SystemRole.Recruiter ? "recruiter" :
                              dto.RoleId == (int)SystemRole.Candidate ? "candidate" : "Không xác định";

            try
            {
                // 5. Update role
                targetUser.RoleId = dto.RoleId;
                targetUser.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateUserAsync(targetUser);
                _cache.Remove($"user-status-{targetUserId}");

                // 6. Revoke sessions to apply new role claim immediately on next login
                await _tokenRepository.RevokeAllUserRefreshTokensAsync(targetUserId);

                // 7. Log success
                string logAction = $"Cập nhật vai trò người dùng {targetUser.Email} từ '{oldRoleName}' thành '{newRoleName}'.";
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    ActivityLogCategory.SECURITY, 
                    actorEmail, 
                    logAction, 
                    ActivityLogStatus.SUCCESS, 
                    ipAddress, 
                    userAgent);

                return new ResponseBase<bool>(true, $"Cập nhật vai trò thành công sang '{newRoleName}'.");
            }
            catch (Exception ex)
            {
                string logAction = $"Lỗi cập nhật vai trò người dùng {targetUser.Email} sang RoleId {dto.RoleId}: {ex.Message}";
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    ActivityLogCategory.SYSTEM, 
                    actorEmail, 
                    logAction, 
                    ActivityLogStatus.FAIL, 
                    ipAddress, 
                    userAgent);

                return new ResponseBase<bool>($"Lỗi hệ thống khi cập nhật vai trò: {ex.Message}");
            }
        }

        public async Task<ResponseBase<PagedResult<UserActivityLogDto>>> GetPagedActivityLogsAsync(
            int page, 
            int pageSize, 
            string? search, 
            ActivityLogCategory? category, 
            ActivityLogStatus? status)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var (items, total) = await _userRepository.GetPagedActivityLogsAsync(page, pageSize, search, category, status);

            var dtos = items.Select(l => new UserActivityLogDto
            {
                Id = l.Id,
                UserId = l.UserId,
                ActorRole = l.ActorRole,
                ActionCategory = l.ActionCategory.ToString(),
                ActorEmail = l.ActorEmail,
                Action = l.Action,
                Status = l.Status.ToString(),
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent,
                CreatedAt = l.CreatedAt
            }).ToList();

            var result = new PagedResult<UserActivityLogDto>
            {
                Items = dtos,
                TotalItems = total,
                Page = page,
                PageSize = pageSize
            };

            return new ResponseBase<PagedResult<UserActivityLogDto>>(result);
        }

        public async Task<UserStatus?> GetUserStatusAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            return user?.Status;
        }

        private async Task LogActivityAsync(
            Guid? userId, 
            string actorRole, 
            ActivityLogCategory category, 
            string actorEmail, 
            string action, 
            ActivityLogStatus status, 
            string ipAddress, 
            string userAgent)
        {
            var log = new UserActivityLogs
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ActorRole = actorRole,
                ActionCategory = category,
                ActorEmail = actorEmail,
                Action = action,
                Status = status,
                IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? "unknown" : ipAddress,
                UserAgent = string.IsNullOrWhiteSpace(userAgent) ? "unknown" : userAgent,
                CreatedAt = DateTime.UtcNow
            };

            await _auditLogRepository.AddActivityLogAsync(log);
        }
    }
}
