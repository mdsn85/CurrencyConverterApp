namespace CurrencyConverterApp.API.Models
{
    public class CurrencyConversionResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public decimal ConvertedAmount { get; set; }
        public string FromCurrency { get; set; }
        public string ToCurrency { get; set; }
        public decimal Amount { get; set; }
        public decimal ConversionRate { get; set; }
        public int StatusCode { get; set; }
    }

}
