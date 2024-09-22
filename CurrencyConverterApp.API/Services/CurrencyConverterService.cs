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
        private readonly string[] _excludedCurrencies = { "TRY", "PLN", "THB", "MXN" };  // Currencies to exclude

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
                throw new HttpRequestException("Error Fetching data from frankfurter API "+ response.ReasonPhrase);
            }
            catch (Exception ex)
            {
                string inerMessage = ex.InnerException != null ? ex.InnerException.Message : "";
                _logger.LogError($"Error Fetching data from frankfurter API {ex.Message} , {inerMessage}");
                throw new HttpRequestException("Error Fetching data from frankfurter API");

            }


        }


        // Method for converting amounts between different currencies
        public async Task<CurrencyConversionResponse> ConvertCurrency(string fromCurrency, string toCurrency, decimal amount)
        {
            // Check if the requested currencies are in the excluded list
            if (_excludedCurrencies.Contains(fromCurrency) || _excludedCurrencies.Contains(toCurrency))
            {
                return new CurrencyConversionResponse
                {
                    Success = false,
                    ErrorMessage = "Conversion between the specified currencies is not allowed.",
                    StatusCode = 400
                };
            }
            var cacheKey = $"CurrencyConversion-{fromCurrency}-{toCurrency}-{amount}";
            if (_cache.TryGetValue(cacheKey, out CurrencyConversionResponse cachedConversion))
            {
                return cachedConversion;
            }

            var client = _httpClientFactory.CreateClient("FrankfurterApi");

            // API call to get the conversion rate
            //amount=10&from=GBP&to=USD

            var response = await client.GetAsync($"latest?amount={amount}&from={fromCurrency}&to={toCurrency}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                //{"amount":10.0,"base":"GBP","date":"2024-09-20","rates":{"USD":13.3071}}
                var rates = JsonConvert.DeserializeObject<ExchangeRatesResponse>(content);

                var convertedAmount = rates.Rates[toCurrency];

                var conversionResult = new CurrencyConversionResponse
                {
                    Success = true,
                    ConvertedAmount = convertedAmount,
                    FromCurrency = fromCurrency,
                    ToCurrency = toCurrency,
                    Amount = amount,
                    //ConversionRate = conversionRate
                };

                // Store the result in memory cache
                _cache.Set(cacheKey, conversionResult, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CalculateTimeUntilNextRefresh()
                });

                return conversionResult;
            }

            _logger.LogError($"Failed to convert currency from {fromCurrency} to {toCurrency}");
            return new CurrencyConversionResponse
            {
                Success = false,
                ErrorMessage = "Error fetching conversion rates.",
                StatusCode = 500
            };
        }

        // Method to fetch historical exchange rates with pagination
        public async Task<PaginatedHistoricalRatesResponse> GetHistoricalRates(string baseCurrency, DateTime startDate, DateTime endDate, int pageNumber, int pageSize)
        {
            var client = _httpClientFactory.CreateClient("FrankfurterApi");

            // Create cache key
            var cacheKey = $"HistoricalRates-{baseCurrency}-{startDate.ToShortDateString()}-{endDate.ToShortDateString()}-{pageNumber}-{pageSize}";

            // Try to get cached result
            if (_cache.TryGetValue(cacheKey, out PaginatedHistoricalRatesResponse cachedRates))
            {
                return cachedRates;
            }

            // Fetch historical rates from the API
            var response = await client.GetAsync($"https://api.frankfurter.app/{startDate:yyyy-MM-dd}..{endDate:yyyy-MM-dd}?base={baseCurrency}");

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var ratesResponse = JsonConvert.DeserializeObject<HistoricalRatesResponse>(content);

                // Perform pagination
                var paginatedResult = PaginateRates(ratesResponse.Rates, pageNumber, pageSize);

                // Cache the result for future use
                _cache.Set(cacheKey, paginatedResult, TimeSpan.FromHours(1));

                return paginatedResult;
            }

            _logger.LogError($"Failed to fetch historical rates for {baseCurrency}");
            return new PaginatedHistoricalRatesResponse
            {
                Success = false,
                ErrorMessage = "Error fetching historical rates."
            };
        }

        // Helper method to perform pagination on the result
        private PaginatedHistoricalRatesResponse PaginateRates(Dictionary<DateTime, Dictionary<string, decimal>> rates, int pageNumber, int pageSize)
        {
            var paginatedRates = rates
                .OrderBy(r => r.Key)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToDictionary(x => x.Key, x => x.Value);

            return new PaginatedHistoricalRatesResponse
            {
                Success = true,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalCount = rates.Count,
                Rates = paginatedRates
            };
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
