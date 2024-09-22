using CurrencyConverterApp.API.Models;

namespace CurrencyConverterApp.API.Services
{
    public interface ICurrencyConverterService
    {
        Task<ExchangeRatesResponse> GetLatestRates(string BaseCurrency);
        Task<CurrencyConversionResponse> ConvertCurrency(string fromCurrency, string toCurrency, decimal amount);

        Task<PaginatedHistoricalRatesResponse> GetHistoricalRates(string baseCurrency, DateTime startDate, DateTime endDate, int pageNumber, int pageSize);


    }
}
