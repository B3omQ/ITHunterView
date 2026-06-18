using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("user_wallets")]
    public class UserWallets
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("balance")]
        public int Balance { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

    }
}
