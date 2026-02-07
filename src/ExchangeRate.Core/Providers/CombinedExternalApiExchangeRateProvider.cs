using System;
using System.Collections.Generic;
using System.Net.Http;
using ExchangeRate.Core.Entities;
using ExchangeRate.Core.Enums;
using ExchangeRate.Core.Helpers;
using ExchangeRate.Core.Models;
using FluentResults;
using ExchangeRateEntity = ExchangeRate.Core.Entities.ExchangeRate;

namespace ExchangeRate.Core.Providers
{
    /// <summary>
    /// This class is a combination of all the functions to fetch the rates daily, weekly, bi-weekly or monthly, having everything here is more manageable since they are all related
    /// </summary>
    public class CombinedExternalApiExchangeRateProvider : ExternalApiExchangeRateProvider
    {
        public const int MaxQueryIntervalInDays = 180;

        public CombinedExternalApiExchangeRateProvider(HttpClient httpClient, ExternalExchangeRateApiConfig externalExchangeRateApiConfig)
            : base(httpClient, externalExchangeRateApiConfig)
        {
        }


        public virtual IEnumerable<ExchangeRateEntity> GetHistoricalDailyFxRates(DateTime from, DateTime to, ForexProviders providers)
        {
            if (to < from)
                throw new ArgumentException("to must be later than or equal to from");

            foreach (var period in GetDateRange(from, to, MaxQueryIntervalInDays))
            {
                var rates = GetDailyRatesAsync(providers, (period.StartDate, period.EndDate)).GetAwaiter().GetResult();

                return rates;
            }

            return new List<ExchangeRateEntity>();
        }

        public virtual IEnumerable<ExchangeRateEntity> GetDailyFxRates(ForexProviders providers)
        {
            // return await GetDailyRatesAsync(providers.BankId));
            // get a longer interval in case some of the previous rates were missed
            return GetHistoricalDailyFxRates(DateTime.UtcNow.Date.AddDays(-4), DateTime.UtcNow.Date, providers);
        }

        public static IEnumerable<(DateTime StartDate, DateTime EndDate)> GetDateRange(DateTime startDate, DateTime endDate, int daysChunkSize)
        {
            DateTime markerDate;

            while ((markerDate = startDate.AddDays(daysChunkSize)) < endDate)
            {
                yield return (StartDate: startDate, EndDate: markerDate.AddDays(-1));
                startDate = markerDate;
            }

            yield return (StartDate: startDate, EndDate: endDate);
        }

        public IEnumerable<ExchangeRateEntity> GetLatestFxRateAsync(ForexProviders providers)
        {
            return GetDailyRatesAsync(providers).GetAwaiter().GetResult();
        }

        public IEnumerable<ExchangeRateEntity> GetHistoricalMonthlyFxRates(DateTime from, DateTime to, ForexProviders providers)
        {
            if (to < from)
                throw new ArgumentException("to must be later than or equal to from");

            var start = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);

            var date = start;
            while (date <= end)
            {
                var rates = GetMonthlyRatesAsync(providers, (date.Year, date.Month)).GetAwaiter().GetResult();
                foreach (var rate in rates)
                {
                    yield return rate;
                }

                date = date.AddMonths(1);
            }
        }

        public IEnumerable<ExchangeRateEntity> GetMonthlyFxRates(ForexProviders providers)
        {

            //This gets the list from the External API
            var result_list = GetMonthlyRatesAsync(providers).GetAwaiter().GetResult();
            foreach (var result in result_list)
            {
                //This returns the list that is returned
                yield return result;
            }
        }

        public IEnumerable<ExchangeRateEntity> GetWeeklyFxRates(ForexProviders providers)
        {
            //This gets the list from the External API
            var result_list = GetWeeklyRatesAsync(providers).GetAwaiter().GetResult();
            foreach (var result in result_list)
            {
                //This returns the list that is returned
                yield return result;
            }
        }

        public IEnumerable<ExchangeRateEntity> GetHistoricalWeeklyFxRates(DateTime from, DateTime to, ForexProviders providers)
        {


            if (to < from)
                throw new ArgumentException("to must be later than or equal to from");

            var start = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);

            var date = start;
            while (date <= end)
            {
                var rates = GetWeeklyRatesAsync(providers, (date.Year, date.Month)).GetAwaiter().GetResult();
                foreach (var rate in rates)
                {
                    yield return rate;
                }

                date = date.AddMonths(1);
            }
        }

        public IEnumerable<ExchangeRateEntity> GetBiWeeklyFxRates(ForexProviders providers)
        {
            //This gets the list from the External API
            var result_list = GetBiWeeklyRatesAsync(providers).GetAwaiter().GetResult();
            foreach (var result in result_list)
            {
                //This returns the list that is returned
                yield return result;
            }
        }

        public IEnumerable<ExchangeRateEntity> GetHistoricalBiWeeklyFxRates(DateTime from, DateTime to, ForexProviders providers)
        {
            if (to < from)
                throw new ArgumentException("to must be later than or equal to from");

            var start = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);

            var date = start;
            while (date <= end)
            {
                var rates = GetBiWeeklyRatesAsync(providers, (date.Year, date.Month)).GetAwaiter().GetResult();
                foreach (var rate in rates)
                {
                    yield return rate;
                }

                date = date.AddMonths(1);
            }
        }

    }
}
