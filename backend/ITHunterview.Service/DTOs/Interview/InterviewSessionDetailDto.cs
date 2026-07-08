using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Interview
{
    public class InterviewSessionDetailDto
    {
        public InterviewSessionDto Session { get; set; } = null!;
        public List<InterviewAnswerDto> Messages { get; set; } = new();
    }
}
