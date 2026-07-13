using System;
using System.ComponentModel.DataAnnotations;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.InterviewQuestionBank
{
    public class UpdateQuestionBankDto
    {
        [Required]
        public int? CategoryId { get; set; }

        public string? Industry { get; set; }

        [Required]
        public string Level { get; set; }

        [Required(ErrorMessage = "Question text is required.")]
        public string QuestionText { get; set; }
    }
}
