public class StockData
{
    public string Ticker { get; set; }
    public List<DateTime> Dates { get; set; } = new List<DateTime>();
    public List<decimal> Prices { get; set; } = new List<decimal>();
    public List<double> Returns { get; set; } = new List<double>();
}