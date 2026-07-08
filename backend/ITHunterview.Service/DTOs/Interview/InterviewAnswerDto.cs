using System;

namespace ITHunterview.Service.DTOs.Interview
{
    public class InterviewAnswerDto
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public Guid? QuestionId { get; set; }
        public Guid? ParentAnswerId { get; set; }
        public string QuestionText { get; set; }
        public string? AudioUrl { get; set; }
        public string? CandidateTranscript { get; set; }
        public string? AiFeedback { get; set; }
        public int? ScoreLogic { get; set; }
        public int? ScoreTech { get; set; }
        public int? ScoreCommunication { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
