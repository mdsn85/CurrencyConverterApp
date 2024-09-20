using CurrencyConverterApp.API.Models;
using CurrencyConverterApp.API.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;



namespace CurrencyConverterApp.API.test
{
    public class CurrencyCoverterServiceTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly Mock<ILogger<CurrencyConverterService>> _loggerMock;

        private readonly ICurrencyConverterService _currencyConverterService;

        public CurrencyCoverterServiceTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _cacheMock = new Mock<IMemoryCache>();
            _loggerMock = new Mock<ILogger<CurrencyConverterService>>();
            _currencyConverterService = new CurrencyConverterService(_httpClientFactoryMock.Object,_cacheMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetLatestRates_ReturnCacheData_WhenAvailable()
        {
            // Arrange
            var baseCurrency = "USD";
            var cacheKey = $"ExchangeRates-{baseCurrency}";

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

            object cacheEntry = cachedRates;  // Create a temporary object to hold the cached data

            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out cacheEntry)).Returns(true);

            //Act
            var result = await _currencyConverterService.GetLatestRates(baseCurrency);

            //Assert 
            Assert.Equal(cachedRates, result);
            _httpClientFactoryMock.Verify(x => x.CreateClient(It.IsAny<string>()), Times.Never);

        }
    }

}
