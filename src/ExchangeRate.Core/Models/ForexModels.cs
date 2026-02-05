using ExchangeRate.Core.Enums;

namespace ExchangeRate.Core.Models
{
    //This is the forex rates object for the rates gotten
    public class FxRatesBySourceFrequencyAndCurrency
    {
        //This is the source the data was gotten from
        public ExchangeRateSources? source { get; set; } = null;
        //This is the update frequency of the source
        public ExchangeRateFrequencies? frequency { get; set; } = null;
        //This is the type of currency gotten from the source
        public CurrencyTypes? currencyType { get; set; } = null;
        //This is the date of the transaction, this can be a direct, indirect or calculated date
        public DateTime transactionDate { get; set; }
        //This is the exchange rate
        public decimal rate { get; set; }

        public FxRatesBySourceFrequencyAndCurrency(ExchangeRateSources source, ExchangeRateFrequencies frequency, CurrencyTypes currencyType, DateTime transactionDate, decimal rate)
        {
            this.source = source;
            this.frequency = frequency;
            this.currencyType = currencyType;
            this.transactionDate = transactionDate;
            this.rate = rate;
        }
    }

    //This is the minimum date object for the rates gotten
    public class MinFxDateBySourceAndFrequency
    {
        //This is the source the data was gotten from
        public ExchangeRateSources? source { get; set; } = null;
        //This is the update frequency of the source
        public ExchangeRateFrequencies? frequency { get; set; } = null;
        //This is the date of the transaction, this can be a direct, indirect or calculated date
        public DateTime transactionDate { get; set; }

        public MinFxDateBySourceAndFrequency(ExchangeRateSources source, ExchangeRateFrequencies frequency, DateTime transactionDate)
        {
            this.source = source;
            this.frequency = frequency;
            this.transactionDate = transactionDate;
        }
    }
}
