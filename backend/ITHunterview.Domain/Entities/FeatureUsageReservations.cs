using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITHunterview.Domain.Entities;

[Table("feature_usage_reservations")]
public class FeatureUsageReservations
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("user_id")]
    public Guid UserId { get; set; }

    [Column("feature_key")]
    public string FeatureKey { get; set; } = string.Empty;

    [Column("reference_id")]
    public Guid ReferenceId { get; set; }

    [Column("source")]
    public string Source { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "Reserved";

    [Column("coin_amount")]
    public int CoinAmount { get; set; }

    [Column("deduct_transaction_id")]
    public Guid? DeductTransactionId { get; set; }

    [Column("refund_transaction_id")]
    public Guid? RefundTransactionId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("captured_at")]
    public DateTime? CapturedAt { get; set; }

    [Column("released_at")]
    public DateTime? ReleasedAt { get; set; }

    [Column("refunded_at")]
    public DateTime? RefundedAt { get; set; }
}
