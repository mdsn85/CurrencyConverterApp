namespace CurrencyConverterApp.API.Models
{
    public class ExchangeRates
    {
        public string Base {get;set;}
        public Dictionary<string,decimal> Rates { get; set; }
        public string Date { get; set; 
        }

    }
}
