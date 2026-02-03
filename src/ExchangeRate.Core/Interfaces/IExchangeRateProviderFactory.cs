using System.Collections.Generic;
using ExchangeRate.Core.Entities;
using ExchangeRate.Core.Enums;
using ExchangeRate.Core.Interfaces.Providers;

namespace ExchangeRate.Core.Interfaces
{
    public interface IExchangeRateProviderFactory
    {
        //bool TryGetExchangeRateProviderByCurrency(CurrencyTypes currency, out IExchangeRateProvider provider);

        ForexProviders get_Supported_Forex_Providers_From_Enumerable (ExchangeRateSources source);
    }
}
