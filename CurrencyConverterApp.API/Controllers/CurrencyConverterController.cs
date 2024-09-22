using CurrencyConverterApp.API.Models;
using CurrencyConverterApp.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverterApp.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CurrencyConverterController : ControllerBase
    {
        private readonly ICurrencyConverterService _currencyConverterService;

        public CurrencyConverterController(ICurrencyConverterService currencyConverterService)
        {
            _currencyConverterService = currencyConverterService;
        }


        [HttpGet("getLatestRates")]
        public async Task<IActionResult> GetLatestRates([FromQuery] LatestRatesRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);  // Return validation errors if the request is invalid
            }

            try
            {
                var rates = await _currencyConverterService.GetLatestRates(request.BaseCurrency);
                return Ok(rates);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("convert")]
        public async Task<IActionResult> ConvertCurrency([FromQuery] ConvertCurrencyRequest request)
        {
            
            var result = await _currencyConverterService.ConvertCurrency(request.FromCurrency, request.ToCurrency, request.Amount);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(new
            {
                fromCurrency = result.FromCurrency,
                toCurrency = result.ToCurrency,
                amount = result.Amount,
                convertedAmount = result.ConvertedAmount,
                conversionRate = result.ConversionRate
            });
        }



        [HttpGet("historical")]
        public async Task<IActionResult> GetHistoricalRates(string baseCurrency, DateTime startDate, DateTime endDate, int pageNumber = 1, int pageSize = 10)
        {
            var result = await _currencyConverterService.GetHistoricalRates(baseCurrency, startDate, endDate, pageNumber, pageSize);

            if (!result.Success)
            {
                return BadRequest(new { message = result.ErrorMessage });
            }

            return Ok(result);
        }



    }
}
