using System;
using System.Collections.Generic;
using ExchangeRate.Core.Entities;
using ExchangeRate.Core.Enums;

namespace ExchangeRate.Core.Interfaces
{
    public interface IExchangeRateRepository
    {
        /// <summary>
        /// Returns the exchange rate from the <paramref name="fromCurrencyCode"/> to the <paramref name="toCurrencyCode"/> on the given <paramref name="date"/>.
        /// It will return a previously valid rate, if the database does not contain rate for the specified <paramref name="date"/>.
        /// It will return NULL if there is no rate at all for the <paramref name="fromCurrencyCode"/> - <paramref name="toCurrencyCode"/> pair.
        /// </summary>
        decimal? GetRate(string fromCurrencyCode, string toCurrencyCode, DateTime date, ExchangeRateSources source, ExchangeRateFrequencies frequency);
    }
}
