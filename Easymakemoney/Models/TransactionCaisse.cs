namespace Easymakemoney.Models
{
    public class TransactionCaisse
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("transactionDate")]
        public DateTime TransactionDate { get; set; }

        [JsonProperty("transactionType")]
        public string TransactionType { get; set; }

        public Color BackgroundColor { get; set; } = Colors.Transparent;
    }
}
