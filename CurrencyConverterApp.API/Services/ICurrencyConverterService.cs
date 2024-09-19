using CurrencyConverterApp.API.Models;

namespace CurrencyConverterApp.API.Services
{
    public interface ICurrencyConverterService
    {
        Task<ExchangeRatesResponse> GetLatestRates(string BaseCurrency);

    }
}
