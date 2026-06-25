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
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.UseCase
{
    public class UserGovernanceUseCase : IUserGovernanceUseCase
    {
        private readonly IUserRepository _userRepository;
        private readonly ITokenRepository _tokenRepository;
        private readonly IMemoryCache _cache;
        private readonly IAuditLogRepository _auditLogRepository;
 
        public UserGovernanceUseCase(IAuditLogRepository auditLogRepository,IUserRepository userRepository, ITokenRepository tokenRepository, IMemoryCache cache)
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
                    : u.RoleId == (int)SystemRole.Candidate 
                        ? ($"{u.CandidateProfile?.FirstName} {u.CandidateProfile?.LastName}").Trim()
                        : u.RoleId == (int)SystemRole.Staff 
                            ? "Staff Account"
                            : u.RoleId == (int)SystemRole.Admin 
                                ? "Admin Account" 
                                : string.Empty,
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
                return new ResponseBase<UserDetailDto>("User does not exist.");
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
            // 1. Fetch target user
            var targetUser = await _userRepository.GetUserByIdAsync(targetUserId);
            if (targetUser == null)
            {
                return new ResponseBase<bool>("User does not exist.");
            }

            // 2. Prevent status changes on Admin accounts
            if (targetUser.RoleId == (int)SystemRole.Admin)
            {
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    ActivityLogCategory.SECURITY, 
                    actorEmail, 
                    $"Failed: Attempt to modify active status of Administrator (Admin) account {targetUser.Email}.", 
                    ActivityLogStatus.FAIL, 
                    ipAddress, 
                    userAgent);
                return new ResponseBase<bool>("Administrator (Admin) accounts cannot have their active status modified.");
            }

            // 3. Prevent self lockout
            if (targetUserId == actorUserId)
            {
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    ActivityLogCategory.SECURITY, 
                    actorEmail, 
                    "Failed: User attempted to modify their own active status.", 
                    ActivityLogStatus.FAIL, 
                    ipAddress, 
                    userAgent);
                return new ResponseBase<bool>("You cannot update your own active status.");
            }

            var oldStatus = targetUser.Status;
            if (dto.Status == oldStatus)
            {
                return new ResponseBase<bool>("User is already in this status.");
            }
            
            try
            {
                // 4. Update status
                targetUser.Status = dto.Status;
                if (dto.Status == UserStatus.INACTIVE || dto.Status == UserStatus.BANNED)
                {
                    targetUser.DeactiveAt = DateTime.UtcNow;
                }
                else if (dto.Status == UserStatus.ACTIVE || dto.Status == UserStatus.PENDING_VERIFICATION)
                {
                    targetUser.DeactiveAt = null;
                }
                targetUser.UpdatedAt = DateTime.UtcNow;

                await _userRepository.UpdateUserAsync(targetUser);
                _cache.Remove($"user-status-{targetUserId}");

                // 4. Revoke active sessions if BANNED or INACTIVE or PENDING_VERIFICATION
                if (dto.Status == UserStatus.INACTIVE || dto.Status == UserStatus.BANNED || dto.Status == UserStatus.PENDING_VERIFICATION)
                {
                    await _tokenRepository.RevokeAllUserRefreshTokensAsync(targetUserId);
                }

                // 5. Log success
                string logAction = $"Updated user status for {targetUser.Email} from {oldStatus} to {dto.Status}. Reason: {dto.Reason}";
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    dto.Status == UserStatus.BANNED ? ActivityLogCategory.SECURITY : ActivityLogCategory.DATA_MUTATION, 
                    actorEmail, 
                    logAction, 
                    ActivityLogStatus.SUCCESS, 
                    ipAddress, 
                    userAgent);

                return new ResponseBase<bool>(true, $"Updated active status to {dto.Status} successfully.");
            }
            catch (Exception ex)
            {
                string logAction = $"Error updating user status for {targetUser.Email} from {oldStatus} to {dto.Status}: {ex.Message}";
                await LogActivityAsync(
                    targetUserId, 
                    actorRole, 
                    ActivityLogCategory.SYSTEM, 
                    actorEmail, 
                    logAction, 
                    ActivityLogStatus.FAIL, 
                    ipAddress, 
                    userAgent);

                return new ResponseBase<bool>($"System error updating status: {ex.Message}");
            }
        }



        public async Task<UserStatus?> GetUserStatusAsync(Guid userId)
        {
            var user = await _userRepository.GetUserByIdAsync(userId);
            return user?.Status;
        }

        public async Task<ResponseBase<Guid>> CreateStaffAccountAsync(
            CreateStaffDto dto, 
            Guid actorUserId, 
            string actorEmail, 
            string actorRole, 
            string ipAddress, 
            string userAgent)
        {
            if (actorRole != "admin")
            {
                return new ResponseBase<Guid>("Only Administrator (Admin) can create staff accounts.");
            }

            if (dto == null)
            {
                return new ResponseBase<Guid>("Invalid data.");
            }

            var existingUser = await _userRepository.GetUserByEmailAsync(dto.Email.Trim().ToLower());
            if (existingUser != null)
            {
                await LogActivityAsync(
                    null, 
                    actorRole, 
                    ActivityLogCategory.SECURITY, 
                    actorEmail, 
                    $"Failed: Attempt to create Staff account with existing email: {dto.Email}.", 
                    ActivityLogStatus.FAIL, 
                    ipAddress, 
                    userAgent);
                return new ResponseBase<Guid>("Email already exists in the system.");
            }

            try
            {
                string passwordHash = PasswordHasher.HashPassword(dto.Password);

                var newStaff = new User
                {
                    Id = Guid.NewGuid(),
                    Email = dto.Email.Trim().ToLower(),
                    PasswordHash = passwordHash,
                    RoleId = (int)SystemRole.Staff,
                    Status = UserStatus.ACTIVE,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _userRepository.AddUserAsync(newStaff);

                await LogActivityAsync(
                    newStaff.Id, 
                    actorRole, 
                    ActivityLogCategory.DATA_MUTATION, 
                    actorEmail, 
                    $"Created Staff account successfully: {newStaff.Email}.", 
                    ActivityLogStatus.SUCCESS, 
                    ipAddress, 
                    userAgent);

                return new ResponseBase<Guid>(newStaff.Id);
            }
            catch (Exception ex)
            {
                await LogActivityAsync(
                    null, 
                    actorRole, 
                    ActivityLogCategory.SYSTEM, 
                    actorEmail, 
                    $"System error creating Staff account {dto.Email}: {ex.Message}", 
                    ActivityLogStatus.FAIL, 
                    ipAddress, 
                    userAgent);
                return new ResponseBase<Guid>($"System error creating Staff account: {ex.Message}");
            }
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
