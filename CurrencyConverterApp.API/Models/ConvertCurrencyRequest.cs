using System.ComponentModel.DataAnnotations;

namespace CurrencyConverterApp.API.Models
{

    public class ConvertCurrencyRequest
    {
        [Required(ErrorMessage = "FromCurrency is required.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "FromCurrency must be exactly 3 characters.")]
        public string FromCurrency { get; set; }

        [Required(ErrorMessage = "ToCurrency is required.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "ToCurrency must be exactly 3 characters.")]
        public string ToCurrency { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        public decimal Amount { get; set; }
    }
}
