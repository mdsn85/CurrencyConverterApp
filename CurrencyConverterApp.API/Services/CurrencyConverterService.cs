using CurrencyConverterApp.API.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using System.Net.Http;
using System.Text;

namespace CurrencyConverterApp.API.Services
{
    public class CurrencyConverterService : ICurrencyConverterService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDistributedCache _cache;
        readonly ILogger<CurrencyConverterService> _logger;

        public CurrencyConverterService(IHttpClientFactory httpClientFactory, IDistributedCache cache, ILogger<CurrencyConverterService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ExchangeRatesResponse> GetLatestRates(string BaseCurrency)
        {
            var cacheKey = $"ExcheangeRates-{BaseCurrency}";
            var cacheRatesBytes = await _cache.GetAsync(cacheKey);
            if (cacheRatesBytes != null )
            {
                var cacheRates = Encoding.UTF8.GetString(cacheRatesBytes);
                return JsonConvert.DeserializeObject<ExchangeRatesResponse>(cacheRates);
            }

            var client = _httpClientFactory.CreateClient("FrankfurterApi");

            var response = await client.GetAsync($"latest?base={BaseCurrency}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                // Calculate time to next 16:00 CET
                var cacheExpiry = CalculateTimeUntilNextRefresh();
                await _cache.SetStringAsync(cacheKey, content, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = casheExpiry// set the expiration until the next 16:00: CET                });
                return JsonConvert.DeserializeObject<ExchangeRatesResponse>(content);
            }

            _logger.LogError($"Failed to fetch exchange rates for {BaseCurrency}");
            throw new HttpRequestException("Error Fetching data from frankfurter API");


        }

        private TimeSpan CalculateTimeUntilNextRefresh()
        {
            throw new NotImplementedException();
        }
    }
}
