using System;

namespace ITHunterview.Service.DTOs.Cv
{
    public class CvResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int? FileSize { get; set; }
        public string FileType { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
        public string ParsedData { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
