using System;
using System.Collections.Generic;
using System.Linq;
using ITHunterview.Service.Interface.Service;

namespace ITHunterview.Service.Service.AiProviders
{
    public class AiProviderFactory : IAiProviderFactory
    {
        private readonly IEnumerable<IAiProvider> _providers;

        public AiProviderFactory(IEnumerable<IAiProvider> providers)
        {
            _providers = providers;
        }

        public IAiProvider GetProvider(string providerName)
        {
            if (string.IsNullOrWhiteSpace(providerName))
            {
                throw new ArgumentException("AI Provider name cannot be empty.", nameof(providerName));
            }

            var provider = _providers.FirstOrDefault(p => 
                p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
            {
                throw new KeyNotFoundException($"AI Provider '{providerName}' is not registered or supported.");
            }

            return provider;
        }
    }
}
