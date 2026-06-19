using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.Auth
{
    public class GoogleAuthRequestDto
    {
        /// <summary>
        /// Google ID Token received from frontend Google Sign-In
        /// </summary>
        [Required]
        public string IdToken { get; set; } = string.Empty;

        /// <summary>
        /// Optional: specify role for new accounts ("candidate" or "recruiter")
        /// Defaults to "candidate" if not provided
        /// </summary>
        public string RoleType { get; set; } = "candidate";
    }
}
