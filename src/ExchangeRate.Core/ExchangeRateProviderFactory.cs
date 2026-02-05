using System;
using System.Collections.Generic;
using System.Linq;
using ExchangeRate.Core.Entities;
using ExchangeRate.Core.Enums;
using ExchangeRate.Core.Interfaces;

namespace ExchangeRate.Core
{
    class ExchangeRateProviderFactory : IExchangeRateProviderFactory
    {
        private readonly IServiceProvider _serviceProvider;
        //This is to reference / instantiate the newly created forex providers class
        private readonly registeredProviders _Providers;

        public ExchangeRateProviderFactory(IServiceProvider serviceProvider, registeredProviders Providers)
        {
            _serviceProvider = serviceProvider;
            _Providers = Providers;
        }

        //This is my new addition as a way to handle adding and removing sources on the fly that will not impact the code much and in theory can be handled completely through a database
        public ForexProviders get_Supported_Forex_Providers_From_Enumerable (ExchangeRateSources source)
        {
            var provider = _Providers.providers.FirstOrDefault(x => x.Source == source);

            if (provider is null)
            {
                throw new NotSupportedException($"Source {source} is not supported.");
            }

            return provider;
        }
    }
}
