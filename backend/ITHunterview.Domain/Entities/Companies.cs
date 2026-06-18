using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("companies")]
    public class Companies
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("tax_code")]
        public string TaxCode { get; set; }

        [Column("headquarters_address")]
        public string HeadquartersAddress { get; set; }

        [Column("industry")]
        public string Industry { get; set; }

        [Column("company_size")]
        public string CompanySize { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("website")]
        public string Website { get; set; }

        [Column("logo_url")]
        public string LogoUrl { get; set; }

        [Column("verification_method")]
        public CompanyVerificationMethod VerificationMethod { get; set; }

        [Column("verification_document_url")]
        public string VerificationDocumentUrl { get; set; }

        [Column("status")]
        public CompanyStatus Status { get; set; }

        [Column("created_by")]
        public Guid? CreatedBy { get; set; }

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

        [Column("deleted_at")]
        public DateTime? DeletedAt { get; set; }

    }
}
