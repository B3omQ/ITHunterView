using System;
using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.JobApplication
{
    public class UpdateApplicationStatusRequestDto
    {
        [Required]
        public ApplicationStatus Status { get; set; }
    }
}
