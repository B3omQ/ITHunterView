using System;
using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.JobSearch
{
    public class SaveJobRequestDto
    {
        [Required]
        public Guid JobId { get; set; }
    }
}
