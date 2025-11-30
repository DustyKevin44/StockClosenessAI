using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;

public static class CsvLoader
{
    public static StockData LoadStockFromCsv(string filePath, int maxDays = 30)
    {
        var prices = new List<decimal>();
        var dates = new List<DateTime>();

        using (var reader = new StreamReader(filePath))
        {
            string headerLine = reader.ReadLine(); // skip header
            if (headerLine == null) return null;

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;

                var parts = line.Split(',');
                if (parts.Length < 6) continue; // must have at least 6 columns

                // Parse date
                if (!DateTime.TryParse(parts[1].Trim(), out DateTime date)) continue;

                // Parse close price
                if (!decimal.TryParse(parts[5].Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out decimal close)) continue;

                prices.Add(close);
                dates.Add(date);
            }
        }

        if (prices.Count == 0) return null;

        // Oldest → newest
        prices.Reverse();
        dates.Reverse();

        // Take last maxDays
        if (prices.Count > maxDays)
        {
            prices = prices.Skip(prices.Count - maxDays).ToList();
            dates = dates.Skip(dates.Count - maxDays).ToList();
        }

        var stock = new StockData
        {
            Ticker = Path.GetFileNameWithoutExtension(filePath),
            Prices = prices,
            Dates = dates,
            Returns = Utils.ComputeReturns(prices) // Use normal or log returns
        };

        return stock;
    }

    public static List<StockData> LoadAllStocksFromFolder(string folderPath, int maxDays = 30)
    {
        var files = Directory.GetFiles(folderPath, "*.csv");
        var stocks = new List<StockData>();

        foreach (var file in files)
        {
            try
            {
                var stock = LoadStockFromCsv(file, maxDays);
                if (stock != null)
                    stocks.Add(stock);

                Console.WriteLine($"Loaded {Path.GetFileNameWithoutExtension(file)} ({stock?.Prices.Count ?? 0} days)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading {file}: {ex.Message}");
            }
        }

        return stocks;
    }
}
