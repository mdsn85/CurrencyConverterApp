﻿using CurrencyConverterApp.API.Models;
using CurrencyConverterApp.API.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System.Net;


namespace CurrencyConverterApp.API.test
{
    public class CurrencyCoverterServiceTests
    {
        private readonly IConfiguration _configuration;

        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly Mock<ILogger<CurrencyConverterService>> _loggerMock;

        private readonly ICurrencyConverterService _currencyConverterService;
        private readonly Mock<ICacheEntry> _cacheEntryMock;

        public CurrencyCoverterServiceTests()
        {

            // In-memory settings to simulate test environment
            var inMemorySettings = new Dictionary<string, string>
            {
                {"UsePolly", "false"} // This will turn off Polly retry logic in your test
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _cacheMock = new Mock<IMemoryCache>();
            _loggerMock = new Mock<ILogger<CurrencyConverterService>>();
            _cacheEntryMock = new Mock<ICacheEntry>();


            _currencyConverterService = new CurrencyConverterService(
                _httpClientFactoryMock.Object,
                _cacheMock.Object,
                _loggerMock.Object);

        }

        [Fact]
        public async Task GetLatestRates_ReturnCacheData_WhenAvailable()
        {
            // Arrange
            var baseCurrency = "USD";
            var cacheKey = $"ExchangeRates-{baseCurrency}";  // Ensure consistent key

            var cachedRates = new ExchangeRatesResponse
            {
                Base = "EUR",
                Rates = new Dictionary<string, decimal>
                {
                    {"USD", 1.1M },
                    {"AUD", 2.3M }
                },
                Date = "2024-09-19",
            };

            object cacheValue = cachedRates;  // Simulate cache hit

            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out cacheValue)).Returns(true); // Return true for cache hit

            // Act
            var result = await _currencyConverterService.GetLatestRates(baseCurrency);

            // Assert 
            Assert.Equal(cachedRates, result);
            _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never); // Ensure no HTTP call is made
        }

        [Fact]
        public async Task GetLatestRates_FetchesData_WhenCacheIsEmpty()
        {
            // Arrange
            var baseCurrency = "EUR";
            var cacheKey = $"ExchangeRates-{baseCurrency}";  // Ensure consistent key

            var cachedRates = new ExchangeRatesResponse
            {
                Base = "EUR",
                Rates = new Dictionary<string, decimal>
                {
                    {"USD", 1.1M },
                    {"AUD", 2.3M }
                },
                Date = "2024-09-19",
            };

            // Create mock response from the Frankfurter API
            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(cachedRates)) // Ensure correct JSON format
            };

            object cacheValue = null; // Simulate cache miss
            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out cacheValue)).Returns(false);  // Simulate cache miss

            // Mock the behavior of CreateEntry (used by Set)
            _cacheMock
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns(_cacheEntryMock.Object);  // Ensure CreateEntry returns a mock cache entry

            // Mock setting the cache entry's value
            _cacheEntryMock.SetupProperty(c => c.Value);
            _cacheEntryMock.Object.Value = cachedRates;

            // Mock HttpClient to return the expected response
            var mockHttpMessageHandler = new MockHttpMessageHandler(responseMessage);
            var httpClient = new HttpClient(mockHttpMessageHandler)
            {
                BaseAddress = new Uri("https://api.frankfurter.app/")  // Set a BaseAddress for HttpClient
            };
            _httpClientFactoryMock.Setup(x => x.CreateClient("FrankfurterApi")).Returns(httpClient);  // Ensure the client is created for the correct named client

            // Act
            var result = await _currencyConverterService.GetLatestRates(baseCurrency);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(cachedRates.Base, result.Base);  // Compare important fields
            _cacheMock.Verify(x => x.CreateEntry(cacheKey), Times.Once);  // Verify that the cache was updated
        }





        [Fact]
        public async Task GetLatestRates_ThrowsHttpRequestException_WhenApiFails()
        {
            // Arrange
            var baseCurrency = "EUR";
            var cacheKey = $"ExchangeRates-{baseCurrency}";
            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest);

            object cacheValue = null; // Simulate cache miss
            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out cacheValue)).Returns(false);  // Simulate cache miss

            var httpClient = new HttpClient(new MockHttpMessageHandler(responseMessage));
            _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act & Assert
            await Assert.ThrowsAsync<HttpRequestException>(() => _currencyConverterService.GetLatestRates(baseCurrency));
        }


        /////////
        /// 
        [Fact]
        public async Task ConvertCurrency_ReturnsConversionResult_WhenSuccessful()
        {
            // Arrange
            var fromCurrency = "GBP";
            var toCurrency = "USD";
            var amount = 10.0m;

            var cacheKey = $"CurrencyConversion-{fromCurrency}-{toCurrency}-{amount}";
            var conversionRatesResponse = new ExchangeRatesResponse
            {
                Base = "GBP",
                Rates = new Dictionary<string, decimal> { { "USD", 13.3071m } }
            };

            var responseMessage = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonConvert.SerializeObject(conversionRatesResponse))
            };

            object cacheValue = null; // Simulate cache miss
            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out cacheValue)).Returns(false); // Simulate cache miss

            // Mock the behavior of CreateEntry (used by Set)
            var mockCacheEntry = new Mock<ICacheEntry>();
            _cacheMock
                .Setup(m => m.CreateEntry(It.IsAny<object>()))
                .Returns(mockCacheEntry.Object);  // Ensure CreateEntry returns a mock cache entry

            // Mock setting the cache entry's value
            mockCacheEntry.SetupProperty(c => c.Value);
            mockCacheEntry.Object.Value = new CurrencyConversionResponse
            {
                Success = true,
                ConvertedAmount = 13.3071m,
                FromCurrency = fromCurrency,
                ToCurrency = toCurrency,
                Amount = amount
            };

            // Mock HttpClient to return the expected response
            var mockHttpMessageHandler = new MockHttpMessageHandler(responseMessage);
            var httpClient = new HttpClient(mockHttpMessageHandler)
            {
                BaseAddress = new Uri("https://api.frankfurter.app/")  // Set a BaseAddress for HttpClient
            };
            _httpClientFactoryMock.Setup(x => x.CreateClient("FrankfurterApi")).Returns(httpClient);  // Ensure the client is created for the correct named client

            // Act
            var result = await _currencyConverterService.ConvertCurrency(fromCurrency, toCurrency, amount);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(13.3071m, result.ConvertedAmount);
            Assert.Equal(fromCurrency, result.FromCurrency);
            Assert.Equal(toCurrency, result.ToCurrency);
            Assert.Equal(amount, result.Amount);

            // Verify that the cache set was invoked
            _cacheMock.Verify(x => x.CreateEntry(It.IsAny<object>()), Times.Once);
        }


        [Fact]
        public async Task ConvertCurrency_ReturnsError_WhenCurrenciesAreExcluded()
        {
            // Arrange
            var fromCurrency = "TRY"; // Excluded currency
            var toCurrency = "USD";
            var amount = 10.0m;

            // Act
            var result = await _currencyConverterService.ConvertCurrency(fromCurrency, toCurrency, amount);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(400, result.StatusCode);
            Assert.Equal("Conversion between the specified currencies is not allowed.", result.ErrorMessage);
        }

        [Fact]
        public async Task ConvertCurrency_ReturnsError_WhenApiCallFails()
        {
            // Arrange
            var fromCurrency = "GBP";
            var toCurrency = "USD";
            var amount = 10.0m;

            var cacheKey = $"CurrencyConversion-{fromCurrency}-{toCurrency}-{amount}";

            var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest); // Simulate failed API call

            object cacheValue = null; // Simulate cache miss
            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out cacheValue)).Returns(false); // Cache miss

            // Mock HttpClient behavior
            var httpClient = new HttpClient(new MockHttpMessageHandler(responseMessage))
            {
                BaseAddress = new Uri("https://api.frankfurter.app/")
            };
            _httpClientFactoryMock.Setup(x => x.CreateClient("FrankfurterApi")).Returns(httpClient);

            // Act
            var result = await _currencyConverterService.ConvertCurrency(fromCurrency, toCurrency, amount);

            // Assert
            Assert.False(result.Success);
            Assert.Equal(500, result.StatusCode);
            Assert.Equal("Error fetching conversion rates.", result.ErrorMessage);
        }

    }

    // MockHttpMessageHandler that always returns the predefined response
    // Mock HttpMessageHandler to simulate HTTP responses
    public class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _responseMessage;

        public MockHttpMessageHandler(HttpResponseMessage responseMessage)
        {
            _responseMessage = responseMessage;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(_responseMessage); // Always return the mocked response
        }
    }
}
