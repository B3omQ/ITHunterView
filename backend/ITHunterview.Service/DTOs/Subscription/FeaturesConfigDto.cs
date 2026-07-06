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
        public int? CvOptimizeLimit { get; set; }

        // Recruiter limits
        public int? ActiveJobPostings { get; set; }
        public int? ActiveSourcingLimit { get; set; }
        public int? HighlightedJobs { get; set; }
        public bool? Analytics { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Role.Equals("CANDIDATE", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!CvMatchLimit.HasValue || CvMatchLimit < -1)
                {
                    yield return new ValidationResult("CvMatchLimit là bắt buộc và phải >= -1 (-1 nghĩa là không giới hạn).", new[] { nameof(CvMatchLimit) });
                }
                if (!MockInterviewLimit.HasValue || MockInterviewLimit < -1)
                {
                    yield return new ValidationResult("MockInterviewLimit là bắt buộc và phải >= -1.", new[] { nameof(MockInterviewLimit) });
                }
                if (!CvOptimizeLimit.HasValue || CvOptimizeLimit < -1)
                {
                    yield return new ValidationResult("CvOptimizeLimit là bắt buộc và phải >= -1.", new[] { nameof(CvOptimizeLimit) });
                }

                if (ActiveJobPostings.HasValue || ActiveSourcingLimit.HasValue || HighlightedJobs.HasValue || Analytics.HasValue)
                {
                    yield return new ValidationResult("Không được cấu hình các hạn mức của Recruiter cho gói Candidate.", 
                        new[] { nameof(ActiveJobPostings), nameof(ActiveSourcingLimit), nameof(HighlightedJobs), nameof(Analytics) });
                }
            }
            else if (Role.Equals("RECRUITER", System.StringComparison.OrdinalIgnoreCase))
            {
                if (!ActiveJobPostings.HasValue || ActiveJobPostings < -1)
                {
                    yield return new ValidationResult("ActiveJobPostings là bắt buộc và phải >= -1.", new[] { nameof(ActiveJobPostings) });
                }
                if (!ActiveSourcingLimit.HasValue || ActiveSourcingLimit < -1)
                {
                    yield return new ValidationResult("ActiveSourcingLimit là bắt buộc và phải >= -1.", new[] { nameof(ActiveSourcingLimit) });
                }
                if (!HighlightedJobs.HasValue || HighlightedJobs < -1)
                {
                    yield return new ValidationResult("HighlightedJobs là bắt buộc và phải >= -1.", new[] { nameof(HighlightedJobs) });
                }
                if (!Analytics.HasValue)
                {
                    yield return new ValidationResult("Trường Analytics là bắt buộc đối với Recruiter.", new[] { nameof(Analytics) });
                }

                if (CvMatchLimit.HasValue || MockInterviewLimit.HasValue || CvOptimizeLimit.HasValue)
                {
                    yield return new ValidationResult("Không được cấu hình các hạn mức của Candidate cho gói Recruiter.", 
                        new[] { nameof(CvMatchLimit), nameof(MockInterviewLimit), nameof(CvOptimizeLimit) });
                }
            }
        }
    }
}
