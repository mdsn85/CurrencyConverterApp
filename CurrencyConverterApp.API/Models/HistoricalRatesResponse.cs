using Newtonsoft.Json;

namespace CurrencyConverterApp.API.Models
{
    public class HistoricalRatesResponse
    {
        public decimal Amount { get; set; }
        public string Base { get; set; }
        [JsonProperty("start_date")]
        public DateTime StartDate { get; set; }
        [JsonProperty("end_date")]
        public DateTime EndDate { get; set; }
        public Dictionary<DateTime, Dictionary<string, decimal>> Rates { get; set; }
    }
}
