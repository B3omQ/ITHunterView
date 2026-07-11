using System;
using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.CvOptimizer
{
    public class OptimizeCvRequestDto
    {
        [Required]
        public Guid CvId { get; set; }

        public string? TargetJdText { get; set; }
    }
}
