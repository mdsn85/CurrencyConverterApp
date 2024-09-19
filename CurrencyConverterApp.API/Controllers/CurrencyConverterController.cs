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

        [HttpGet("getLatestRate")]
        public async Task<IActionResult> getLatestRate(string baseCurrency)
        {
            try
            {
                var rates = await _currencyConverterService.GetLatestRates(baseCurrency);
                return Ok(rates);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



    }
}
