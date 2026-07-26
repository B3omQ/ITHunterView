using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.Subscription
{
    public class FeaturesConfigDto : IValidatableObject
    {
        [Required(ErrorMessage = "Vai trò (Role) cấu hình tính năng là bắt buộc.")]
        [RegularExpression("^(CANDIDATE|RECRUITER)$", ErrorMessage = "Role phải là CANDIDATE hoặc RECRUITER.")]
        public string Role { get; set; } = string.Empty;

        // Candidate limits
        public int? CvMatchLimit { get; set; }
        public int? MockInterviewLimit { get; set; }
        public int? LearningPathSlotLimit { get; set; }
        public bool? AiRefreshUnlimited { get; set; }
        public bool? PremiumBadge { get; set; }

        // Recruiter limits
        public int? JobSlots { get; set; }
        public int? JobExtendLimit { get; set; }
        public int? UnlockCvLimit { get; set; }
        public int? PushTopLimit { get; set; }
        
        // Common
        public int? CoinCredit { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role.Equals("CANDIDATE", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!CvMatchLimit.HasValue || CvMatchLimit < -1)
                    yield return new ValidationResult("CvMatchLimit là bắt buộc và phải >= -1.", new[] { nameof(CvMatchLimit) });
                
                if (!MockInterviewLimit.HasValue || MockInterviewLimit < -1)
                    yield return new ValidationResult("MockInterviewLimit là bắt buộc và phải >= -1.", new[] { nameof(MockInterviewLimit) });
                
                if (!LearningPathSlotLimit.HasValue || LearningPathSlotLimit < -1)
                    yield return new ValidationResult("LearningPathSlotLimit là bắt buộc và phải >= -1.", new[] { nameof(LearningPathSlotLimit) });
                
                if (!AiRefreshUnlimited.HasValue)
                    yield return new ValidationResult("AiRefreshUnlimited là bắt buộc đối với Candidate.", new[] { nameof(AiRefreshUnlimited) });
                
                if (!PremiumBadge.HasValue)
                    yield return new ValidationResult("PremiumBadge là bắt buộc đối với Candidate.", new[] { nameof(PremiumBadge) });
                
                if (!CoinCredit.HasValue || CoinCredit < 0)
                    yield return new ValidationResult("CoinCredit là bắt buộc và phải >= 0.", new[] { nameof(CoinCredit) });

                if (JobSlots.HasValue || JobExtendLimit.HasValue || UnlockCvLimit.HasValue || PushTopLimit.HasValue)
                {
                    yield return new ValidationResult("Không được cấu hình các hạn mức của Recruiter cho gói Candidate.", 
                        new[] { nameof(JobSlots), nameof(JobExtendLimit), nameof(UnlockCvLimit), nameof(PushTopLimit) });
                }
            }
            else if (Role.Equals("RECRUITER", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!JobSlots.HasValue || JobSlots < -1)
                    yield return new ValidationResult("JobSlots là bắt buộc và phải >= -1.", new[] { nameof(JobSlots) });
                
                if (!JobExtendLimit.HasValue || JobExtendLimit < -1)
                    yield return new ValidationResult("JobExtendLimit là bắt buộc và phải >= -1.", new[] { nameof(JobExtendLimit) });
                
                if (!UnlockCvLimit.HasValue || UnlockCvLimit < -1)
                    yield return new ValidationResult("UnlockCvLimit là bắt buộc và phải >= -1.", new[] { nameof(UnlockCvLimit) });
                
                if (!PushTopLimit.HasValue || PushTopLimit < -1)
                    yield return new ValidationResult("PushTopLimit là bắt buộc và phải >= -1.", new[] { nameof(PushTopLimit) });

                if (!CoinCredit.HasValue || CoinCredit < 0)
                    yield return new ValidationResult("CoinCredit là bắt buộc và phải >= 0.", new[] { nameof(CoinCredit) });

                if (CvMatchLimit.HasValue || MockInterviewLimit.HasValue || LearningPathSlotLimit.HasValue || AiRefreshUnlimited.HasValue || PremiumBadge.HasValue)
                {
                    yield return new ValidationResult("Không được cấu hình các hạn mức của Candidate cho gói Recruiter.", 
                        new[] { nameof(CvMatchLimit), nameof(MockInterviewLimit), nameof(LearningPathSlotLimit), nameof(AiRefreshUnlimited), nameof(PremiumBadge) });
                }
            }
        }
    }
}
