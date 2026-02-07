using ExchangeRate.Api.Infrastructure;
using ExchangeRate.Core;
using ExchangeRate.Core.Entities;
using ExchangeRate.Core.Enums;
using ExchangeRate.Core.Infrastructure;
using ExchangeRate.Core.Interfaces;
using ExchangeRate.Core.Models;
using ExchangeRate.Core.Providers;

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

builder.Configuration.AddJsonFile("RegisteredProviders.json",
    optional: false,
    reloadOnChange: true);

//This is the configuration for the registered providers json file so it can read it into the app and use it
builder.Services.AddSingleton<registeredProviders>(sp =>
{
    var config = builder.Configuration.GetSection("registeredProviders").Get<registeredProviders>();
    return config;
});

// Register HttpClient for providers
builder.Services.AddHttpClient<CombinedExternalApiExchangeRateProvider>();

//This is to be able to use the combined class for fetching daily - weekly rates
builder.Services.AddSingleton<CombinedExternalApiExchangeRateProvider>(sp =>
{
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

//This is to handle the logging to the console so we can monitor the app
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// TEST: Try to resolve the service manually
using (var scope = app.Services.CreateScope())
{
    try
    {
        var service = scope.ServiceProvider.GetRequiredService<registeredProviders>();
        Console.WriteLine("✓ Service resolved successfully!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"✗ Failed to resolve service: {ex.Message}");
    }
}

// GET /api/rates?from={currency}&to={currency}&date={date}&source={source}&frequency={frequency}
app.MapGet("/api/rates", (
    string? from,
    string? to,
    DateTime? date,
    ExchangeRateSources? source,
    ExchangeRateFrequencies? frequency,
    IExchangeRateRepository repository) =>
{
    //This is just a validation reigon to provide a response to the client if the parameters are missing
    #region Validation

    //This is a placeholder for the documentation
    string visit_documentation = "Documentation: https://api.documentation.example.com";

    if (string.IsNullOrEmpty(from))
    {
        return Results.BadRequest(new { error = "Pls provide a currency code for the parameter from. E.g USD. " + visit_documentation });
    }
    if (string.IsNullOrEmpty(to))
    {
        return Results.BadRequest(new { error = "Pls provide a currency code for the parameter to. E.g USD. " + visit_documentation });
    }
    if (date == default || date == null)
    {
        return Results.BadRequest(new { error = "Please provide the date to receive the exchange rate for that specific day E.g 2024-01-11. " + visit_documentation });
    }
    if (source == null)
    {
        return Results.BadRequest(new { error = "Please provide a source to get the exchange rate from. E.g ECB. " + visit_documentation });
    }
    if (frequency == null)
    {
        return Results.BadRequest(new { error = "Please provide the update frequency from the source you are using. E.g Daily. " + visit_documentation });
    }
    #endregion


    var rate = repository.GetRate(from, to, date.Value, source.Value, frequency.Value);

    if (rate == null)
    {
        return Results.NotFound(new { error = $"No exchange rate found for {from} to {to} on {date:yyyy-MM-dd}" });
    }

    return Results.Ok(new ExchangeRateResponse(from, to, date.Value, source.Value.ToString(), frequency.Value.ToString(), rate.Value));
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