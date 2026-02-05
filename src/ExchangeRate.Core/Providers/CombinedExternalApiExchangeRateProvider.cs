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


        public virtual async IAsyncEnumerable<ExchangeRateEntity> GetHistoricalDailyFxRates(DateTime from, DateTime to, ForexProviders providers)
        {
            if (to < from)
                throw new ArgumentException("to must be later than or equal to from");

            foreach (var period in GetDateRange(from, to, MaxQueryIntervalInDays))
            {
                var rates = await GetDailyRatesAsync(providers, (period.StartDate, period.EndDate));
                foreach (var rate in rates)
                {
                    yield return rate;
                }
            }
        }

        public virtual IEnumerable<ExchangeRateEntity> GetDailyFxRates(ForexProviders providers)
        {
            // return await GetDailyRatesAsync(providers.BankId));
            // get a longer interval in case some of the previous rates were missed
            return (IEnumerable<ExchangeRateEntity>)GetHistoricalDailyFxRates(DateTime.UtcNow.Date.AddDays(-4), DateTime.UtcNow.Date, providers);
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

        public async Task<IEnumerable<ExchangeRateEntity>> GetLatestFxRateAsync(ForexProviders providers)
        {


            return await GetDailyRatesAsync(providers);
        }

        public async IAsyncEnumerable<ExchangeRateEntity> GetHistoricalMonthlyFxRates(DateTime from, DateTime to, ForexProviders providers)
        {
            if (to < from)
                throw new ArgumentException("to must be later than or equal to from");

            var start = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);

            var date = start;
            while (date <= end)
            {
                var rates = await GetMonthlyRatesAsync(providers, (date.Year, date.Month));
                foreach (var rate in rates)
                {
                    yield return rate;
                }

                date = date.AddMonths(1);
            }
        }

        public async IAsyncEnumerable<ExchangeRateEntity> GetMonthlyFxRates(ForexProviders providers)
        {

            //This gets the list from the External API
            var result_list = await GetMonthlyRatesAsync(providers);
            foreach (var result in result_list)
            {
                //This returns the list that is returned
                yield return result;
            }
        }

        public async IAsyncEnumerable<ExchangeRateEntity> GetWeeklyFxRates(ForexProviders providers)
        {
            //This gets the list from the External API
            var result_list = await GetWeeklyRatesAsync(providers);
            foreach (var result in result_list)
            {
                //This returns the list that is returned
                yield return result;
            }
        }

        public async IAsyncEnumerable<ExchangeRateEntity> GetHistoricalWeeklyFxRates(DateTime from, DateTime to, ForexProviders providers)
        {


            if (to < from)
                throw new ArgumentException("to must be later than or equal to from");

            var start = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);

            var date = start;
            while (date <= end)
            {
                var rates = await GetWeeklyRatesAsync(providers, (date.Year, date.Month));
                foreach (var rate in rates)
                {
                    yield return rate;
                }

                date = date.AddMonths(1);
            }
        }

        public async IAsyncEnumerable<ExchangeRateEntity> GetBiWeeklyFxRates(ForexProviders providers)
        {
            //This gets the list from the External API
            var result_list = await GetBiWeeklyRatesAsync(providers);
            foreach (var result in result_list)
            {
                //This returns the list that is returned
                yield return result;
            }
        }

        public async IAsyncEnumerable<ExchangeRateEntity> GetHistoricalBiWeeklyFxRates(DateTime from, DateTime to, ForexProviders providers)
        {
            if (to < from)
                throw new ArgumentException("to must be later than or equal to from");

            var start = new DateTime(from.Year, from.Month, 1);
            var end = new DateTime(to.Year, to.Month, 1);

            var date = start;
            while (date <= end)
            {
                var rates = await GetBiWeeklyRatesAsync(providers, (date.Year, date.Month));
                foreach (var rate in rates)
                {
                    yield return rate;
                }

                date = date.AddMonths(1);
            }
        }

    }
}
