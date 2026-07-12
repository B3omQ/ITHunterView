using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.InterviewQuestionBank
{
    public class QuestionBankDto
    {
        public Guid Id { get; set; }
        public int? CategoryId { get; set; }
        public string? Industry { get; set; }
        public string? Level { get; set; }
        public string QuestionText { get; set; }
    }
}
