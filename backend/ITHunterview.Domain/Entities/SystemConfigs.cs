using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("system_configs")]
    public class SystemConfigs
    {
        [Key]
        [Column("config_key")]
        public string ConfigKey { get; set; }

        [Column("config_value")]
        public string ConfigValue { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("updated_by")]
        public Guid? UpdatedBy { get; set; }

    }
}
