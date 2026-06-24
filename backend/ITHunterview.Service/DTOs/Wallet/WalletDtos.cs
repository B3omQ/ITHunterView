using System;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Wallet
{
    public class WalletBalanceDto
    {
        public Guid UserId { get; set; }
        public int Balance { get; set; }
    }

    public class WalletTransactionDto
    {
        public Guid Id { get; set; }
        public int Amount { get; set; }
        public string TransactionType { get; set; } = null!;
        public Guid? ReferenceId { get; set; }
        public string Description { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePaymentDto
    {
        public string TargetId { get; set; } = null!;
        public PaymentTargetType TargetType { get; set; }
        public PaymentGateway PaymentGateway { get; set; }
    }

    public class PaymentDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = null!;
        public int? CreditsGranted { get; set; }
        public string PaymentGateway { get; set; } = null!;
        public string GatewayTransactionId { get; set; } = null!;
        public string TargetType { get; set; } = null!;
        public Guid? TargetId { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class PaymentSimulationDto
    {
        public Guid PaymentId { get; set; }
        public bool Success { get; set; }
        public string GatewayTransactionId { get; set; } = null!;
    }
}
