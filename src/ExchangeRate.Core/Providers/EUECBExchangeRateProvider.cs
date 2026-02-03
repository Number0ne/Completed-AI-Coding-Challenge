using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using ExchangeRate.Core.Enums;
using ExchangeRate.Core.Helpers;
using ExchangeRate.Core.Interfaces.Providers;
using ExchangeRate.Core.Models;
using ExchangeRateEntity = ExchangeRate.Core.Entities.ExchangeRate;
/*
namespace ExchangeRate.Core.Providers
{
    /// <summary>
    /// European Central Bank (ECB) exchange rate provider.
    /// Provides both daily and monthly EUR exchange rates.
    /// </summary>
    public class EUECBExchangeRateProvider : ExternalApiExchangeRateProvider, IDailyExchangeRateProvider, IMonthlyExchangeRateProvider
    {
        public EUECBExchangeRateProvider(HttpClient httpClient, ExternalExchangeRateApiConfig externalExchangeRateApiConfig)
            : base(httpClient, externalExchangeRateApiConfig)
        {
        }

        public override CurrencyTypes Currency => CurrencyTypes.EUR;

        public override QuoteTypes QuoteType => QuoteTypes.Indirect;

        public override ExchangeRateSources Source => ExchangeRateSources.ECB;

        public override string BankId => "EUECB";

        public IEnumerable<ExchangeRateEntity> GetHistoricalDailyFxRates(DateTime from, DateTime to)
        {
            if (to < from)
                throw new ArgumentException("to must be later than or equal to from");

            foreach (var period in DailyExternalApiExchangeRateProvider.GetDateRange(from, to, DailyExternalApiExchangeRateProvider.MaxQueryIntervalInDays))
            {
                var rates = AsyncUtil.RunSync(() => GetDailyRatesAsync(BankId, (period.StartDate, period.EndDate)));
                foreach (var rate in rates)
                {
                    yield return rate;
                }
            }
        }

        public IEnumerable<ExchangeRateEntity> GetDailyFxRates()
        {
            // return AsyncUtil.RunSync(() => GetDailyRatesAsync(BankId));
            // get a longer interval in case some of the previous rates were missed
            return GetHistoricalDailyFxRates(DateTime.UtcNow.Date.AddDays(-4), DateTime.UtcNow.Date);
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

        public async Task<IEnumerable<ExchangeRateEntity>> GetLatestFxRateAsync()
        {
            return await GetDailyRatesAsync(BankId);
        }
    }
}*/
