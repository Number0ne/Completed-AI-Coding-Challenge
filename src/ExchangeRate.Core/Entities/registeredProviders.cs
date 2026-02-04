using System.Collections.Generic;
using ExchangeRate.Core.Enums;
using ExchangeRate.Core.Interfaces;

namespace ExchangeRate.Core.Entities;

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
};

public class registeredProviders
{
    public static List<ForexProviders> InitialProvidersData => new List<ForexProviders>()
    {
        new ForexProviders()
        {
            ///This example using ECB we see that the base currency is Euros and the Quote type is indirect also we see that the source updates daily and weekly and we can use this to get the methods to use and fetch from the source
            BankId = "EUECB",
            Currency = CurrencyTypes.EUR,
            QuoteType = QuoteTypes.Indirect,
            Source = ExchangeRateSources.ECB,
            updatesDaily = true,
            updatesWeekly = false,
            updatesBiWeekly = false,
            updatesMonthly = true
        },
        new ForexProviders()
        {
            BankId = "GBHMRC",
            Currency = CurrencyTypes.GBP,
            QuoteType = QuoteTypes.Indirect,
            Source = ExchangeRateSources.HMRC,
            updatesDaily = false,
            updatesWeekly = false,
            updatesBiWeekly = false,
            updatesMonthly = true
        },
        new ForexProviders()
        {
            BankId = "HUCB",
            Currency = CurrencyTypes.HUF,
            QuoteType = QuoteTypes.Direct,
            Source = ExchangeRateSources.MNB,
            updatesDaily = true,
            updatesWeekly = false,
            updatesBiWeekly = false,
            updatesMonthly = false
        },
        new ForexProviders()
        {
            BankId = "MXCB",
            Currency = CurrencyTypes.MXN,
            QuoteType = QuoteTypes.Direct,
            Source = ExchangeRateSources.MXCB,
            updatesDaily = false,
            updatesWeekly = false,
            updatesBiWeekly = false,
            updatesMonthly = true
        },
        new ForexProviders()
        {
            BankId = "PLCB",
            Currency = CurrencyTypes.PLN,
            QuoteType = QuoteTypes.Direct,
            Source = ExchangeRateSources.PLCB,
            updatesDaily = true,
            updatesWeekly = false,
            updatesBiWeekly = false,
            updatesMonthly = false
        },
        new ForexProviders()
        {
            BankId = "SECB",
            Currency = CurrencyTypes.SEK,
            QuoteType = QuoteTypes.Direct,
            Source = ExchangeRateSources.SECB,
            updatesDaily = true,
            updatesWeekly = false,
            updatesBiWeekly = false,
            updatesMonthly = false
        }
    };

    //This is a simple function in the class that will get all the registered providers who can provide forex values
    public List<ForexProviders> getList()
    {
        return InitialProvidersData;
    }
}
