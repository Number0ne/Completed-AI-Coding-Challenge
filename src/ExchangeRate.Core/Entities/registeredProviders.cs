using System.Collections.Generic;
using ExchangeRate.Core.Enums;
using ExchangeRate.Core.Interfaces;

namespace ExchangeRate.Core.Entities
{
    //I've moved the providers into a class of their own for reusability and also so I can easily swap any of the providers out on the fly or add extra parameters to them for use in calculation
    public class ForexProviders
    {
        public CurrencyTypes Currency { get; set; }

        public QuoteTypes QuoteType { get; set; }

        public ExchangeRateSources Source { get; set; }

        public string BankId { get; set; }

        public bool updatesDaily { get; set; }
        public bool updatesWeekly { get; set; }
        public bool updatesBiWeekly { get; set; }
        public bool updatesMonthly { get; set; }
        //This is one to be used to convert the rate from monthly or weekly to daily
        public bool ConvertToDailyRate { get; set; }
    };

    public class registeredProviders
    {
        public List<ForexProviders> providers { get; set; }
    };
}
