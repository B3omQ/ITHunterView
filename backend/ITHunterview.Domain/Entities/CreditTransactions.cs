using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("credit_transactions")]
    public class CreditTransactions
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("wallet_id")]
        public Guid WalletId { get; set; }

        [Column("amount")]
        public int Amount { get; set; }

        [Column("transaction_type")]
        public CreditTransactionType TransactionType { get; set; }

        [Column("reference_id")]
        public Guid? ReferenceId { get; set; }

        [Column("description")]
        public string Description { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

    }
}
