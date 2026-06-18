using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("interview_answers")]
    public class InterviewAnswers
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("session_id")]
        public Guid SessionId { get; set; }

        [Column("question_id")]
        public Guid? QuestionId { get; set; }

        [Column("parent_answer_id")]
        public Guid? ParentAnswerId { get; set; }

        [Column("question_text")]
        public string QuestionText { get; set; }

        [Column("audio_url")]
        public string AudioUrl { get; set; }

        [Column("candidate_transcript")]
        public string CandidateTranscript { get; set; }

        [Column("ai_feedback")]
        public string AiFeedback { get; set; }

        [Column("score_logic")]
        public int? ScoreLogic { get; set; }

        [Column("score_tech")]
        public int? ScoreTech { get; set; }

        [Column("score_communication")]
        public int? ScoreCommunication { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

    }
}
