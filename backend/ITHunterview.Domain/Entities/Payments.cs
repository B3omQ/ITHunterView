using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities
{
    [Table("payments")]
    public class Payments
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Column("user_id")]
        public Guid UserId { get; set; }

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("currency")]
        public string Currency { get; set; }

        [Column("credits_granted")]
        public int? CreditsGranted { get; set; }

        [Column("payment_gateway")]
        public PaymentGateway PaymentGateway { get; set; }

        [Column("gateway_transaction_id")]
        public string GatewayTransactionId { get; set; }

        [Column("target_type")]
        public PaymentTargetType TargetType { get; set; }

        [Column("target_id")]
        public Guid? TargetId { get; set; }

        [Column("status")]
        public PaymentStatus Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; }

    }
}
