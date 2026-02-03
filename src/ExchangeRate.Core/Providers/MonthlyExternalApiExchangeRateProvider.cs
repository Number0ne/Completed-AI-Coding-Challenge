using System;
using System.Collections.Generic;
using System.Net.Http;
using ExchangeRate.Core.Helpers;
using ExchangeRate.Core.Interfaces.Providers;
using ExchangeRate.Core.Models;
using ExchangeRateEntity = ExchangeRate.Core.Entities.ExchangeRate;
/*
namespace ExchangeRate.Core.Providers
{
    /// <summary>
    /// Base class for providers that fetch monthly exchange rates.
    /// </summary>
    public abstract class MonthlyExternalApiExchangeRateProvider : ExternalApiExchangeRateProvider, IMonthlyExchangeRateProvider
    {
        protected MonthlyExternalApiExchangeRateProvider(HttpClient httpClient, ExternalExchangeRateApiConfig externalExchangeRateApiConfig)
            : base(httpClient, externalExchangeRateApiConfig)
        {
        }

        public IEnumerable<ExchangeRateEntity> GetHistoricalMonthlyFxRates(DateTime from, DateTime to)
        {
            if (to < from)
                throw new ArgumentException("to must be later than or equal to from");

            var start = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);

            var date = start;
            while (date <= end)
            {
                var rates = AsyncUtil.RunSync(() => GetMonthlyRatesAsync(BankId, (date.Year, date.Month)));
                foreach (var rate in rates)
                {
                    yield return rate;
                }

                date = date.AddMonths(1);
            }
        }

        public IEnumerable<ExchangeRateEntity> GetMonthlyFxRates()
        {
            return AsyncUtil.RunSync(() => GetMonthlyRatesAsync(BankId));
        }
    }
}*/
