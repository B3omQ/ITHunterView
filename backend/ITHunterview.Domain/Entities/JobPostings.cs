using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace ITHunterview.Domain.Entities
{
    [Table("job_postings")]
    public class JobPostings
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("job_code")]
        public string JobCode { get; set; }

        [Column("title_embedding", TypeName = "vector(768)")]
        public Vector? TitleEmbedding { get; set; }

        [Column("skills_embedding", TypeName = "vector(768)")]
        public Vector? SkillsEmbedding { get; set; }

        [Column("experience_embedding", TypeName = "vector(768)")]
        public Vector? ExperienceEmbedding { get; set; }

        [Column("domain_embedding", TypeName = "vector(768)")]
        public Vector? DomainEmbedding { get; set; }

        [Column("parsed_data", TypeName = "jsonb")]
        public string? ParsedData { get; set; }

        [Column("recruiter_id")]
        public Guid RecruiterId { get; set; }

        [Column("company_id")]
        public Guid CompanyId { get; set; }

        [Column("title")]
        public string Title { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("responsibilities")]
        public string Responsibilities { get; set; }

        [Column("requirements")]
        public string Requirements { get; set; }

        [Column("benefits")]
        public string Benefits { get; set; }

        [Column("min_salary")]
        public decimal? MinSalary { get; set; }

        [Column("max_salary")]
        public decimal? MaxSalary { get; set; }

        [Column("currency")]
        public string Currency { get; set; }

        [Column("location")]
        public string Location { get; set; }

        [Column("detailed_location")]
        public string? DetailedLocation { get; set; }

        [Column("status")]
        public JobStatus Status { get; set; }

        [Column("level")]
        public string? Level { get; set; }

        [Column("working_model")]
        public string? WorkingModel { get; set; }

        [Column("job_expertise")]
        public string? JobExpertise { get; set; }

        [Column("job_domain", TypeName = "text[]")]
        public List<string>? JobDomain { get; set; }

        [Column("application_count")]
        public int ApplicationCount { get; set; }

        [Column("view_count")]
        public int ViewCount { get; set; }

        [Column("published_at")]
        public DateTime? PublishedAt { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

    }
}
