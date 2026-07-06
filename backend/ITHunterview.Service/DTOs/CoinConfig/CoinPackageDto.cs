namespace ITHunterview.Service.DTOs.CoinConfig
{
    public class CoinPackageDto
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Coins { get; set; }
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }
}
