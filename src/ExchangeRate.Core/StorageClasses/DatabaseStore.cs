using System;
using System.Collections.Generic;
using ExchangeRate.Core.Entities;
using ExchangeRate.Core.Enums;
using ExchangeRate.Core.Infrastructure;

namespace ExchangeRate.Api.Infrastructure
{
    //This class has been created and the contents of program have been moved into it so when it is time to upgrade to a database then this class can be edited without interfering with the program
    public class MemoryDataStore
    {
        /// <summary>
        /// In-memory implementation of IExchangeRateDataStore.
        /// Candidates can replace this with a real database implementation (e.g., EF Core).
        /// </summary>
        public class InMemoryExchangeRateDataStore : IExchangeRateDataStore
        {
            private readonly List<ExchangeRate.Core.Entities.ExchangeRate> _exchangeRates = new();
            private readonly List<ExchangeRate.Core.Entities.PeggedCurrency> _peggedCurrencies = new();

            public IQueryable<ExchangeRate.Core.Entities.ExchangeRate> ExchangeRates => _exchangeRates.AsQueryable();

            public Task<List<ExchangeRate.Core.Entities.ExchangeRate>> GetExchangeRatesAsync(DateTime minDate, DateTime maxDate)
            {
                var rates = _exchangeRates
                    .Where(r => r.Date.HasValue && r.Date.Value >= minDate && r.Date.Value < maxDate)
                    .ToList();

                return Task.FromResult(rates);
            }

            public Task SaveExchangeRatesAsync(IEnumerable<ExchangeRate.Core.Entities.ExchangeRate> rates)
            {
                foreach (var rate in rates)
                {
                    var existingRate = _exchangeRates.FindIndex(r =>
                        r.Date == rate.Date &&
                        r.CurrencyId == rate.CurrencyId &&
                        r.Source == rate.Source &&
                        r.Frequency == rate.Frequency);

                    //This section has been modified so if an existing rate is found then it updates the value of the existing rate
                    if (existingRate < 0)
                    {
                        _exchangeRates.Add(rate);
                    }
                    else
                    {
                        _exchangeRates[existingRate].Rate = rate.Rate;
                    }
                }

                return Task.CompletedTask;
            }

            public List<ExchangeRate.Core.Entities.PeggedCurrency> GetPeggedCurrencies()
            {
                return _peggedCurrencies.ToList();
            }

            public void AddPeggedCurrency(ExchangeRate.Core.Entities.PeggedCurrency peggedCurrency)
            {
                _peggedCurrencies.Add(peggedCurrency);
            }
        }
    }
}