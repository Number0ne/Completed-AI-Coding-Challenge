using ExchangeRate.Api.Infrastructure;
using ExchangeRate.Core;
using ExchangeRate.Core.Entities;
using ExchangeRate.Core.Enums;
using ExchangeRate.Core.Infrastructure;
using ExchangeRate.Core.Interfaces;
using ExchangeRate.Core.Models;
using ExchangeRate.Core.Providers;
using static ExchangeRate.Api.Infrastructure.MemoryDataStore;

var builder = WebApplication.CreateBuilder(args);

// Configure ExchangeRate services
builder.Services.AddSingleton<ExternalExchangeRateApiConfig>(sp =>
{
    var config = builder.Configuration.GetSection("ExchangeRateApi").Get<ExternalExchangeRateApiConfig>();
    return config ?? new ExternalExchangeRateApiConfig
    {
        BaseAddress = builder.Configuration["ExchangeRateApi:BaseAddress"] ?? "http://localhost",
        TokenEndpoint = builder.Configuration["ExchangeRateApi:TokenEndpoint"] ?? "/connect/token",
        ClientId = builder.Configuration["ExchangeRateApi:ClientId"] ?? "client",
        ClientSecret = builder.Configuration["ExchangeRateApi:ClientSecret"] ?? "secret"
    };
});

// Register HttpClient for providers
builder.Services.AddHttpClient<CombinedExternalApiExchangeRateProvider>();

//This is to be able to use the combined class for fetching daily - weekly rates
builder.Services.AddSingleton<CombinedExternalApiExchangeRateProvider>(sp => {
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(CombinedExternalApiExchangeRateProvider));
    var config = sp.GetRequiredService<ExternalExchangeRateApiConfig>();
    return new CombinedExternalApiExchangeRateProvider(httpClient, config);
});

// Register the provider factory
builder.Services.AddSingleton<IExchangeRateProviderFactory, ExchangeRateProviderFactory>();

// Register the data store - this can be replaced by candidates with a real DB implementation
builder.Services.AddSingleton<IExchangeRateDataStore, InMemoryExchangeRateDataStore>();

// Register the repository
builder.Services.AddSingleton<IExchangeRateRepository, ExchangeRateRepository>();

// Register the forex registered providers service
builder.Services.AddSingleton<registeredProviders>();

var app = builder.Build();

// GET /api/rates?from={currency}&to={currency}&date={date}&source={source}&frequency={frequency}
app.MapGet("/api/rates", (
    string from,
    string to,
    DateTime date,
    ExchangeRateSources source,
    ExchangeRateFrequencies frequency,
    IExchangeRateRepository repository) =>
{
    var rate = repository.GetRate(from, to, date, source, frequency);

    if (rate == null)
    {
        return Results.NotFound(new { error = $"No exchange rate found for {from} to {to} on {date:yyyy-MM-dd}" });
    }

    return Results.Ok(new ExchangeRateResponse(from, to, date, source.ToString(), frequency.ToString(), rate.Value));
});

app.Run();

/// <summary>
/// Response model for the exchange rate API endpoint.
/// </summary>
public record ExchangeRateResponse(
    string FromCurrency,
    string ToCurrency,
    DateTime Date,
    string Source,
    string Frequency,
    decimal Rate);

// Make Program accessible to test project
public partial class Program { }