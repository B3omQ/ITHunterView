using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ITHunterview.Domain.Enums;

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



        [Column("level")]
        public string? Level { get; set; }

        [Column("industry")]
        public string? Industry { get; set; }

        [Column("question_text")]
        public string QuestionText { get; set; }



        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

    }
}
