using ExchangeRate.Core.Enums;

namespace ExchangeRate.Core.Interfaces.Providers;

public interface IExchangeRateProvider
{
    ExchangeRateCalculationMethods DefaultCalculationMethod => this switch
    {
        IDailyExchangeRateProvider => ExchangeRateCalculationMethods.Daily,
        IMonthlyExchangeRateProvider => ExchangeRateCalculationMethods.Monthly,
        IBiWeeklyExchangeRateProvider => ExchangeRateCalculationMethods.BiWeekly,
        IWeeklyExchangeRateProvider => ExchangeRateCalculationMethods.Weekly,
        _ => ExchangeRateCalculationMethods.Daily
    };

    ExchangeRateFrequencies DefaultCalculationFrequency => this switch
    {
        IDailyExchangeRateProvider => ExchangeRateFrequencies.Daily,
        IMonthlyExchangeRateProvider => ExchangeRateFrequencies.Monthly,
        IBiWeeklyExchangeRateProvider => ExchangeRateFrequencies.BiWeekly,
        IWeeklyExchangeRateProvider => ExchangeRateFrequencies.Weekly,
        _ => ExchangeRateFrequencies.Daily
    };
}
