using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("permissions")]
    public class Permissions
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("action")]
        public string Action { get; set; }

        [Column("resource")]
        public string Resource { get; set; }

    }
}
