using CurrencyConverterApp.API.Models;
//using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;


namespace CurrencyConverterApp.API.Services
{
    public class CurrencyConverterService : ICurrencyConverterService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        //private readonly IDistributedCache _cache;
        readonly ILogger<CurrencyConverterService> _logger;

        public CurrencyConverterService(IHttpClientFactory httpClientFactory, IMemoryCache cache, ILogger<CurrencyConverterService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
            _logger = logger;
        }

        public async Task<ExchangeRatesResponse> GetLatestRates(string BaseCurrency)
        {
            try
            {

                var cacheKey = $"ExchangeRates-{BaseCurrency}";
                // Try to get the cached rates from in-memory cache
                if (_cache.TryGetValue(cacheKey, out ExchangeRatesResponse cachedRates))
                {
                    return cachedRates; // Return cached result if available
                }

                var client = _httpClientFactory.CreateClient("FrankfurterApi");

                var response = await client.GetAsync($"latest?base={BaseCurrency}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var rates = JsonConvert.DeserializeObject<ExchangeRatesResponse>(content);

                    var cacheExpiry = CalculateTimeUntilNextRefresh();

                    // Store the result in memory cache with expiration policy
                    _cache.Set(cacheKey, rates, new MemoryCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = cacheExpiry // set the expiration until the next 16:00: CET
                    });

                    return rates;
                }

                _logger.LogError($"Failed to fetch exchange rates for {BaseCurrency}");
                throw new HttpRequestException("Error Fetching data from frankfurter API");
            }
            catch (Exception ex)
            {
                string inerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                _logger.LogError($"Error Fetching data from frankfurter API {ex.Message} , {inerMessage}");
                throw new HttpRequestException("Error Fetching data from frankfurter API");

            }


        }

        private TimeSpan CalculateTimeUntilNextRefresh()
        {
            var utcNow = DateTime.UtcNow;
            var cetTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");
            var currentCETTime = TimeZoneInfo.ConvertTimeFromUtc(utcNow, cetTimeZone);

            //define the time of the next refresh (16:00 CET)
            var nextRefreshTime = new DateTime(currentCETTime.Year, currentCETTime.Month, currentCETTime.Day, 16, 0, 0);

            //if it is past refresh time for today, schedule the next refresh for tommorow 
            if (currentCETTime >= nextRefreshTime)
            {
                nextRefreshTime = nextRefreshTime.AddDays(1);
            }

            //if the next refresh falls on weekend, move it to next monday
            while (nextRefreshTime.DayOfWeek == DayOfWeek.Saturday || nextRefreshTime.DayOfWeek == DayOfWeek.Sunday)
            {
                nextRefreshTime = nextRefreshTime.AddHours(1);
            }

            return nextRefreshTime - currentCETTime;
        }
    }
}
