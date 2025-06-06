namespace BlazorApp.Shared
{
    public class StockData
    {
        public string Symbol { get; set; } = "";
        public double Price { get; set; }
        public System.DateTime LastUpdated { get; set; }
    }
}