using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.Wallet
{
    /// <summary>
    /// Independent VND price for one custom top-up Coin. It is not derived from coin packages.
    /// </summary>
    public class CustomCoinTopupPriceDto
    {
        public int PricePerCoinVnd { get; set; }
    }

    public class CreateCustomCoinTopupDto
    {
        public int CoinAmount { get; set; }
        public PaymentGateway PaymentGateway { get; set; }
    }
}
