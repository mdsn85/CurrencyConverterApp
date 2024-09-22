using System.ComponentModel.DataAnnotations;

namespace CurrencyConverterApp.API.Models
{
    public class LatestRatesRequest
    {
        [Required(ErrorMessage = "BaseCurrency is required.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "BaseCurrency must be exactly 3 characters.")]
        public string BaseCurrency { get; set; }

    }
}
