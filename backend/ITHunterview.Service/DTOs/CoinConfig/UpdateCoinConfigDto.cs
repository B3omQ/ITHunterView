using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.CoinConfig
{
    public class UpdateCoinConfigDto
    {
        public CoinFeatureCostsDto FeatureCosts { get; set; } = new();
        public List<CoinPackageDto> Packages { get; set; } = new();
    }
}
