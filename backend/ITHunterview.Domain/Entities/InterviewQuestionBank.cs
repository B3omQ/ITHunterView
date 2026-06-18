using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("interview_question_bank")]
    public class InterviewQuestionBank
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("category_id")]
        public int? CategoryId { get; set; }

        [Column("difficulty")]
        public DifficultyLevel Difficulty { get; set; }

        [Column("question_text")]
        public string QuestionText { get; set; }

        [Column("sample_answer")]
        public string SampleAnswer { get; set; }

        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

    }
}
