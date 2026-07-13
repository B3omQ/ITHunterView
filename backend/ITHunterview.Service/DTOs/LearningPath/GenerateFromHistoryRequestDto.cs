using System;

namespace ITHunterview.Service.DTOs.LearningPath
{
    public class GenerateFromHistoryRequestDto
    {
        /// <summary>
        /// ID của bản ghi matching CV-JD cụ thể (CvJobMatchScores.Id).
        /// Nếu null → tự động lấy bản ghi Completed mới nhất của candidate.
        /// </summary>
        public Guid? MatchScoreId { get; set; }

        /// <summary>
        /// ID của buổi phỏng vấn thử cụ thể (InterviewSessions.Id).
        /// Nếu null → tự động lấy session Completed mới nhất của candidate.
        /// </summary>
        public Guid? SessionId { get; set; }

        /// <summary>
        /// Thời gian mong muốn hoàn thành lộ trình (tuần). Mặc định 12 tuần.
        /// </summary>
        public int TimeframeInWeeks { get; set; } = 12;
    }
}
