using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public static class AlphaVantageHelper
{
    private static readonly HttpClient client = new HttpClient();

    public static async Task<List<decimal>> GetDailyPrices(string ticker, string apiKey, int days = 30)
    {
        string url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={ticker}&apikey={apiKey}&outputsize=compact";
        var response = await client.GetStringAsync(url);
        var json = JObject.Parse(response);

        var timeSeries = json["Time Series (Daily)"];
        if (timeSeries == null)
            throw new Exception($"No data found for {ticker}");

        var prices = new List<decimal>();

        // Sort by date ascending
        var sorted = timeSeries.Children<JProperty>()
            .OrderBy(p => DateTime.Parse(p.Name));

        foreach (var prop in sorted)
        {
            decimal close = decimal.Parse(prop.Value["5. adjusted close"].ToString());
            prices.Add(close);
        }

        prices.Reverse(); // Oldest → newest

        // Take last N days
        if (prices.Count > days)
            prices = prices.Skip(prices.Count - days).ToList();

        return prices;
    }
}