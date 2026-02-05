using Microsoft.Extensions.Logging;
using FluentResults;
using ExchangeRate.Core.Exceptions;
using ExchangeRate.Core.Helpers;
using ExchangeRate.Core.Interfaces;
using ExchangeRate.Core.Entities;
using ExchangeRate.Core.Enums;
using ExchangeRate.Core.Infrastructure;
using ExchangeRate.Core.Providers;
using System.Linq.Expressions;
using ExchangeRate.Core.Models;
using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace ExchangeRate.Core
{
    class ExchangeRateRepository : IExchangeRateRepository
    {
        private static readonly IEnumerable<ExchangeRateSources> SupportedSources = System.Enum.GetValues(typeof(ExchangeRateSources)).Cast<ExchangeRateSources>().ToList();

        /// <summary>
        /// Maps currecy code string to currency type.
        /// </summary>
        private static readonly Dictionary<string, CurrencyTypes> CurrencyMapping;
        //Changed this to now be an object list so it's easy to follow
        private List<FxRatesBySourceFrequencyAndCurrency> _fxRatesBySourceFrequencyAndCurrency;
        //Changed this to now be an object list so it's easy to follow
        private List<MinFxDateBySourceAndFrequency> _minFxDateBySourceAndFrequency;
        private readonly Dictionary<CurrencyTypes, PeggedCurrency> _peggedCurrencies;

        private readonly IExchangeRateDataStore _dataStore;

        private readonly ILogger<ExchangeRateRepository> _logger;
        private readonly IExchangeRateProviderFactory _exchangeRateSourceFactory;

        private readonly CombinedExternalApiExchangeRateProvider _providersCombinedExchange;

        static ExchangeRateRepository()
        {
            var currencies = System.Enum.GetValues(typeof(CurrencyTypes)).Cast<CurrencyTypes>().ToList();
            CurrencyMapping = currencies.ToDictionary(x => x.ToString().ToUpperInvariant());
        }

        private void ResetMinFxDates()
        {
            _minFxDateBySourceAndFrequency = SupportedSources.SelectMany(x => new List<MinFxDateBySourceAndFrequency>
            {
                new (x, ExchangeRateFrequencies.Daily, DateTime.MaxValue),
                new (x, ExchangeRateFrequencies.Monthly, DateTime.MaxValue),
                new (x, ExchangeRateFrequencies.Weekly, DateTime.MaxValue),
                new (x, ExchangeRateFrequencies.BiWeekly, DateTime.MaxValue)
            }).ToList() ?? new List<MinFxDateBySourceAndFrequency>(); ;
        }

        public ExchangeRateRepository(IExchangeRateDataStore dataStore, ILogger<ExchangeRateRepository> logger, IExchangeRateProviderFactory exchangeRateSourceFactory, CombinedExternalApiExchangeRateProvider providersCombinedExchange)
        {
            _dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exchangeRateSourceFactory = exchangeRateSourceFactory ?? throw new ArgumentNullException(nameof(exchangeRateSourceFactory));
            _providersCombinedExchange = providersCombinedExchange;

            _fxRatesBySourceFrequencyAndCurrency = new List<FxRatesBySourceFrequencyAndCurrency>();
            ResetMinFxDates();

            _peggedCurrencies = _dataStore.GetPeggedCurrencies()
                .ToDictionary(x => x.CurrencyId!.Value);
        }

        internal ExchangeRateRepository(IEnumerable<Entities.ExchangeRate> rates, IExchangeRateProviderFactory exchangeRateSourceFactory)
        {
            _fxRatesBySourceFrequencyAndCurrency = new List<FxRatesBySourceFrequencyAndCurrency>();
            ResetMinFxDates();

            LoadRates(rates);

            _exchangeRateSourceFactory = exchangeRateSourceFactory ?? throw new ArgumentNullException(nameof(exchangeRateSourceFactory));
        }

        /// <summary>
        /// Returns the exchange rate for the <paramref name="toCurrency"/> on the given <paramref name="date"/>.
        /// It will return a previously valid rate, if the database does not contain rate for the specified <paramref name="date"/>.
        /// It will return NULL if there is no rate at all for the <paramref name="toCurrency"/>.
        /// </summary>
        public decimal? GetRate(string fromCurrencyCode, string toCurrencyCode, DateTime date, ExchangeRateSources source, ExchangeRateFrequencies frequency)
        {
            try
            {
                var fromCurrency = GetCurrencyType(fromCurrencyCode);

                var toCurrency = GetCurrencyType(toCurrencyCode);

                var provider = _exchangeRateSourceFactory.get_Supported_Forex_Providers_From_Enumerable(source);

                if (toCurrency == fromCurrency)
                    return 1m;

                date = date.Date;

                var minFxDate = GetMinFxDate(date, provider, frequency);

                // If neither fromCurrency, nor toCurrency matches the provider's currency, we need to calculate cross rates
                if (fromCurrency != provider.Currency && toCurrency != provider.Currency)
                {
                    return GetRate(fromCurrency.ToString(), provider.Currency.ToString(), date, source, frequency) * GetRate(provider.Currency.ToString(), toCurrency.ToString(), date, source, frequency);
                }

                CurrencyTypes lookupCurrency = default;
                var result = GetFxRate(GetRatesByCurrency(source, frequency), date, minFxDate, provider, fromCurrency, toCurrency, out _);


                if (result.IsSuccess)
                    return result.Value;

                // If no fx rate found for date, update rates in case some dates are missing between minFxDate and tax point date
                if (result.Errors.FirstOrDefault() is NoFxRateFoundError)
                {
                    UpdateRates(provider, minFxDate, date, frequency);

                    result = GetFxRate(GetRatesByCurrency(source, frequency), date, minFxDate, provider, fromCurrency,
                        toCurrency, out var currency);

                    if (result.IsSuccess)
                        return result.Value;

                    lookupCurrency = currency;
                }

                _logger.LogError("No {source} {frequency} exchange rate found for {lookupCurrency} on {date:yyyy-MM-dd}. Earliest available date: {minFxDate:yyyy-MM-dd}. FromCurrency: {fromCurrency}, ToCurrency: {toCurrency}", source, frequency, lookupCurrency, date, minFxDate, fromCurrency, toCurrency);
                return null;
            }
            catch (Exception ex)
            {
                File.AppendAllText(@"/home/debian/Documents/Personal Projects/TestLogs.txt", ex.Message + ex.StackTrace);
                return null;
            }
        }

        /// <summary>
        /// Updates the exchange rates for the last available day/month.
        /// </summary>
        public void UpdateRates(ForexProviders provider)
        {
            try
            {
                var rates = new List<Entities.ExchangeRate>();

                if (provider.updatesDaily)
                    rates.AddRange(_providersCombinedExchange.GetDailyFxRates(provider).ToList());

                if (provider.updatesMonthly)
                    rates.AddRange(_providersCombinedExchange.GetMonthlyFxRates(provider).ToList());

                if (provider.updatesWeekly)
                    rates.AddRange(_providersCombinedExchange.GetWeeklyFxRates(provider).ToList());

                if (provider.updatesBiWeekly)
                    rates.AddRange(_providersCombinedExchange.GetBiWeeklyFxRates(provider).ToList());

                if (rates.Any())
                {
                    LoadRatesFromDb(PeriodHelper.GetStartOfMonth(rates.Min(x => x.Date!.Value)));

                    var itemsToSave = new List<Entities.ExchangeRate>();
                    foreach (var rate in rates)
                    {
                        if (AddRateToDictionaries(rate))
                            itemsToSave.Add(rate);
                    }

                    if (itemsToSave.Any())
                        _dataStore.SaveExchangeRatesAsync(itemsToSave).GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update daily rates for {source}", provider.Source.ToString());
            }
        }

        private DateTime GetMinFxDate(DateTime date, ForexProviders provider, ExchangeRateFrequencies frequency)
        {
            //MinFxDate is just the name placeholder given to the MinFxDateBySourceAndFrequency object that is returned

            var Forex_Object = _minFxDateBySourceAndFrequency.FirstOrDefault(s => s.source == provider.Source && s.frequency == frequency);

            if (Forex_Object == default)
                throw new ExchangeRateException("Couldn't find base min FX date for source: " + provider.Source);

            var minFxDate = Forex_Object.transactionDate;

            //This sets the minimum possible date which will usually be the start of the month for the date given
            DateTime minDate = PeriodHelper.GetStartOfMonth(date);

            // if the currently available date is higher than the requested date, then we need to get it from the database, or fill the database from the FX rate source
            if (minFxDate > date)
            {
                //simplified this aspect since it'll just pass the values into update rates and validation occurs there
                EnsureMinimumDateRange(provider, minDate, frequency);
            }

            // Update minFxDate value after EnsureMinimumDateRange
            Forex_Object = _minFxDateBySourceAndFrequency.FirstOrDefault(s => s.source == provider.Source && s.frequency == frequency);

            if (Forex_Object == default)
                throw new ExchangeRateException("Couldn't find base min FX date for source: " + provider.Source);

            minFxDate = Forex_Object.transactionDate;

            return minFxDate;
        }

        /// <summary>
        /// Ensures that the database contains all exchange rates after <paramref name="minDate"/>.
        /// </summary>
        public bool EnsureMinimumDateRange(ForexProviders provider, DateTime minDate, ExchangeRateFrequencies frequency)
        {

            //First we check if the source is available with the required update frequency. By default it should be but this is an exception handler

            //MinFxDate is just the name placeholder given to the MinFxDateBySourceAndFrequency object that is returned
            var Forex_Object = _minFxDateBySourceAndFrequency.FirstOrDefault(s => s.source == provider.Source && s.frequency == frequency);

            if (Forex_Object == default)
                throw new ExchangeRateException("Couldn't find base min FX date for source: " + provider.Source);

            var minFxDate = Forex_Object.transactionDate;
            //If the minfxdate is availabl then we get the start of the month for that paritular minimum forex date value
            minFxDate = PeriodHelper.GetStartOfMonth(minFxDate);

            // if the minimum exchange rate date is lower than or equal to the specified date, then we don't need to update the rates
            if (minFxDate <= minDate)
                return true;

            //If its not then we load the rates from the database and then try again to check if the min Forex Date is available
            LoadRatesFromDb(minDate);

            //again we check if the minimum date has been updated with values from our database. By default it should be but this is an exception handler

            Forex_Object = _minFxDateBySourceAndFrequency.FirstOrDefault(s => s.source == provider.Source && s.frequency == frequency);

            if (Forex_Object == default)
                throw new ExchangeRateException("Couldn't find base min FX date for source: " + provider.Source);

            minFxDate = Forex_Object.transactionDate;

            if (minFxDate == default)
                throw new ExchangeRateException($"Couldn't find min FX date for source {provider.Source} with frequency {frequency}");

            if (minFxDate <= minDate)
                return true;

            if (minFxDate == DateTime.MaxValue)
            {
                minFxDate = DateTime.UtcNow.Date;
            }

            // if there would still be missing FX rates, we need to collect them from the historical data source
            return UpdateRates(provider, minDate, minFxDate, frequency);
        }

        private bool UpdateRates(ForexProviders provider, DateTime minDate, DateTime minFxDate, ExchangeRateFrequencies frequency)
        {
            var itemsToSave = new List<Entities.ExchangeRate>();

            // Ensure dates are in the correct chronological order (from <= to)
            var from = minDate <= minFxDate ? minDate : minFxDate;
            var to = minDate <= minFxDate ? minFxDate : minDate;

            switch (frequency)
            {
                case ExchangeRateFrequencies.Daily:
                    if (!provider.updatesDaily)
                        throw new ExchangeRateException($"Provider {provider} does not support frequency {frequency}");
                    itemsToSave.AddRange(_providersCombinedExchange.GetHistoricalDailyFxRates(from, to, provider).ToList());
                    break;
                case ExchangeRateFrequencies.Monthly:
                    if (!provider.updatesMonthly)
                        throw new ExchangeRateException($"Provider {provider} does not support frequency {frequency}");
                    itemsToSave.AddRange(_providersCombinedExchange.GetHistoricalMonthlyFxRates(from, to, provider).ToList());
                    break;
                case ExchangeRateFrequencies.Weekly:
                    if (!provider.updatesWeekly)
                        throw new ExchangeRateException($"Provider {provider} does not support frequency {frequency}");
                    itemsToSave.AddRange(_providersCombinedExchange.GetHistoricalWeeklyFxRates(from, to, provider).ToList());
                    break;
                case ExchangeRateFrequencies.BiWeekly:
                    if (!provider.updatesBiWeekly)
                        throw new ExchangeRateException($"Provider {provider} does not support frequency {frequency}");
                    itemsToSave.AddRange(_providersCombinedExchange.GetHistoricalBiWeeklyFxRates(from, to, provider).ToList());
                    break;
                default:
                    throw new ExchangeRateException($"Unsupported frequency: {frequency}");
            }

            if (itemsToSave.Count == 0)
            {
                _logger.LogError("No historical data found between date {minDate:yyyy-MM-dd} and {v:yyyy-MM-dd} for source {source} with frequency {frequency}.", minDate, minFxDate, provider.Source, frequency);
                return false;
            }

            var newMinFxDate = minFxDate;
            foreach (var item in itemsToSave.ToArray())
            {
                if (!AddRateToDictionaries(item))
                    itemsToSave.Remove(item);

                if (item.Date!.Value < newMinFxDate)
                    newMinFxDate = item.Date.Value;
            }


            int Forex_Object_Index = _minFxDateBySourceAndFrequency.FindIndex(s => s.source == provider.Source && s.frequency == frequency);

            if (Forex_Object_Index < 0)
                throw new ExchangeRateException("Couldn't find base min FX date for source: " + provider.Source);

            //This updates the minimum date on the forex object with the specified source and frequency
            _minFxDateBySourceAndFrequency[Forex_Object_Index].transactionDate = newMinFxDate;

            // if storing in memory was successful, we can save it to the database
            if (itemsToSave.Any())
                _dataStore.SaveExchangeRatesAsync(itemsToSave).GetAwaiter().GetResult();

            return true;
        }

        /// <summary>
        /// Loads FX rates into cache dictionary starting with the specified date and sets the <see cref="_minFxDate"/>.
        /// </summary>
        private void LoadRatesFromDb(DateTime minDate)
        {
            //Gets the minimum forex date
            var minFxDate = _minFxDateBySourceAndFrequency.OrderByDescending(s => s.transactionDate).First().transactionDate;

            //Retrieves the relevant exchange rates
            var fxRatesInDb = _dataStore.GetExchangeRatesAsync(minDate, minFxDate).GetAwaiter().GetResult();

            LoadRates(fxRatesInDb);
        }

        /// <summary>
        /// Loads FX rates into cache dictionary starting with the specified date and sets the <see cref="_minFxDateBySourceAndFrequency"/>.
        /// </summary>
        private void LoadRates(IEnumerable<Entities.ExchangeRate> fxRatesInDb)
        {
            // store them in memory and refresh minimum FX rate date
            var minFxDateBySource = _minFxDateBySourceAndFrequency;
            foreach (var item in fxRatesInDb)
            {
                AddRateToDictionaries(item);

                var source = item.Source!.Value;
                var frequency = item.Frequency!.Value;

                var Forex_Object = _minFxDateBySourceAndFrequency.FirstOrDefault(s => s.source == source && s.frequency == frequency);

                if (Forex_Object == default)
                    throw new ExchangeRateException("Couldn't find base min FX date for source: " + source);

                var minFxDate = Forex_Object.transactionDate; ;

                if (minFxDate == default)
                    throw new ExchangeRateException($"Couldn't find min FX date for source {source} with frequency {frequency}");

                if (item.Date!.Value < minFxDate)
                {
                    //This initializes an index to use and update the items in the list
                    int foundindex = _minFxDateBySourceAndFrequency.FindIndex(s => s.source == source && s.frequency == frequency);

                    //After finding the index of the object we are looking for (The first occurence) we then check add the new minimum date to the item
                    _minFxDateBySourceAndFrequency[foundindex].transactionDate = item.Date.Value;
                }
            }
        }

        /// <summary>
        /// Adds exchange rates to the FX rate dictionaries.
        /// It should be called to every currency-date pairs once.
        /// </summary>
        private bool AddRateToDictionaries(Entities.ExchangeRate item)
        {
            var currency = item.CurrencyId!.Value;
            var date = item.Date!.Value;
            var source = item.Source!.Value;
            var frequency = item.Frequency!.Value;
            var newRate = item.Rate!.Value;

            var returned_forex_item = _fxRatesBySourceFrequencyAndCurrency.FirstOrDefault(s => s.source == source && s.frequency == frequency && s.currencyType == currency && s.transactionDate == date);

            //check if the default date is what is returned from the default items list
            if (returned_forex_item == default)
            {
                _fxRatesBySourceFrequencyAndCurrency.Add(
                    new FxRatesBySourceFrequencyAndCurrency(source, frequency, currency, date, newRate));
            }
            else if (decimal.Round(newRate, Entities.ExchangeRate.Precision) != decimal.Round(returned_forex_item.rate, Entities.ExchangeRate.Precision))
            {

                //This is where I'll update the exchange rate with the new value that is just provided cause the assumption is that the new rate is always the most up to date

                //This initializes an index to use and update the items in the list
                int foundindex = _fxRatesBySourceFrequencyAndCurrency.FindIndex(s => s.source == source && s.frequency == frequency && s.currencyType == currency);

                //After finding the index of the object we are looking for (The first occurence) we then check add the new minimum date to the item
                _fxRatesBySourceFrequencyAndCurrency[foundindex].rate = newRate;

                return true;

                //_logger.LogError("Saved exchange rate differs from new value. Currency: {currency}. Saved rate: {savedRate}. New rate: {newRate}. Source: {source}. Frequency: {frequency}", currency, savedRate, newRate, source, frequency);
                //throw new ExchangeRateException($"_fxRatesByCurrency already contains rate for {currency}-{date:yyyy-MMdd}. Source: {source}. Frequency: {frequency}");
            }

            return false;
        }

        private Result<decimal> GetFxRate(
            IReadOnlyList<FxRatesBySourceFrequencyAndCurrency> ratesByCurrencyAndDate,
            DateTime date,
            DateTime minFxDate,
            ForexProviders provider,
            CurrencyTypes fromCurrency,
            CurrencyTypes toCurrency,
            out CurrencyTypes lookupCurrency)
        {
            // Handle same-currency conversion (can happen in recursive pegged currency lookups)
            if (fromCurrency == toCurrency)
            {
                lookupCurrency = fromCurrency;
                return Result.Ok(1m);
            }

            //  always need to find the rate for the currency that is not the provider's currency
            lookupCurrency = toCurrency == provider.Currency ? fromCurrency : toCurrency;
            var nonLookupCurrency = toCurrency == provider.Currency ? toCurrency : fromCurrency;

            //This is so I can use the lookup currency in my functions
            var internal_lookupCurrency = lookupCurrency;

            var currencyObj = ratesByCurrencyAndDate.FirstOrDefault(s => s.currencyType == internal_lookupCurrency);

            var currencyDict = currencyObj?.rate;

            if (currencyObj == default)
            {
                if (!_peggedCurrencies.TryGetValue(lookupCurrency, out var peggedCurrency))
                {
                    return Result.Fail(new NotSupportedCurrencyError(lookupCurrency));
                }

                var peggedToCurrencyResult = GetFxRate(ratesByCurrencyAndDate, date, minFxDate, provider, nonLookupCurrency, peggedCurrency.PeggedTo!.Value, out _);

                if (peggedToCurrencyResult.IsFailed)
                {
                    return peggedToCurrencyResult;
                }

                var peggedRate = peggedCurrency.Rate!.Value;
                var resultRate = peggedToCurrencyResult.Value;

                return Result.Ok(toCurrency == provider.Currency
                    ? peggedRate / resultRate
                    : resultRate / peggedRate);

            }
            // start looking for the date, and decreasing the date if no match found (but only until the minFxDate)

            for (var d = date; d >= minFxDate; d = d.AddDays(-1d))
            {
                var new_forex_object_to_check = ratesByCurrencyAndDate.FirstOrDefault(s => s.currencyType == internal_lookupCurrency);

                if (new_forex_object_to_check?.transactionDate == d)
                {
                    var temporary_rate_to_return = new_forex_object_to_check.rate;
                    /*
                       If your local currency is EUR:
                       - Direct exchange rate: 1 USD = 0.92819 EUR
                       - Indirect exchange rate: 1 EUR = 1.08238 USD
                    */

                    // QuoteType    ProviderCurrency    FromCurrency    ToCurrency    Rate
                    // Direct       EUR                 USD             EUR           fxRate
                    // Direct       EUR                 EUR             USD           1/fxRate
                    // InDirect     EUR                 USD             EUR           1/fxRate
                    // InDirect     EUR                 EUR             USD           fxRate

                    return provider.QuoteType switch
                    {
                        QuoteTypes.Direct when toCurrency == provider.Currency => Result.Ok(temporary_rate_to_return),
                        QuoteTypes.Direct when fromCurrency == provider.Currency => Result.Ok(1 / temporary_rate_to_return),
                        QuoteTypes.Indirect when fromCurrency == provider.Currency => Result.Ok(temporary_rate_to_return),
                        QuoteTypes.Indirect when toCurrency == provider.Currency => Result.Ok(1 / temporary_rate_to_return),
                        _ => throw new InvalidOperationException("Unsupported QuoteType")
                    };
                }
            }

            return Result.Fail(new NoFxRateFoundError());
        }

        private static CurrencyTypes GetCurrencyType(string currencyCode)
        {
            if (string.IsNullOrWhiteSpace(currencyCode))
                throw new ExchangeRateException("Null or empty currency code.");

            if (!CurrencyMapping.TryGetValue(currencyCode.ToUpperInvariant(), out var currency))
                throw new ExchangeRateException("Not supported currency code: " + currencyCode);

            return currency;
        }

        private IReadOnlyList<FxRatesBySourceFrequencyAndCurrency> GetRatesByCurrency(ExchangeRateSources source, ExchangeRateFrequencies frequency)
        {
            var returned_forex_item = _fxRatesBySourceFrequencyAndCurrency.Where(s => s.source == source && s.frequency == frequency).ToList();


            var returned_forex_item_date = returned_forex_item.FirstOrDefault()?.transactionDate;

            //check if the default date is what is returned from the default items list
            if (returned_forex_item_date == default)
                throw new ExchangeRateException(
                    $"No exchange rates available for source {source} with frequency {frequency}");

            return new ReadOnlyCollection<FxRatesBySourceFrequencyAndCurrency>(returned_forex_item);
        }
    }

    class NotSupportedCurrencyError : Error
    {
        public NotSupportedCurrencyError(CurrencyTypes currency)
            : base("Not supported currency: " + currency) { }
    }

    class NoFxRateFoundError : Error
    {
        public NoFxRateFoundError()
            : base("No fx rate found") { }
    }
}
