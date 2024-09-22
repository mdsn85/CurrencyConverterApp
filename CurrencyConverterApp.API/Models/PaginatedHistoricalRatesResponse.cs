namespace CurrencyConverterApp.API.Models
{
    public class PaginatedHistoricalRatesResponse
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public Dictionary<DateTime, Dictionary<string, decimal>> Rates { get; set; }
    }
}
