using System;
using System.Linq;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        string folderPath = "Data"; // Folder containing all CSVs
        int maxDays = 30;           // Last N days to use

        // Load all stocks once
        var stocks = CsvLoader.LoadAllStocksFromFolder(folderPath, maxDays);

        if (!stocks.Any())
        {
            Console.WriteLine("No stocks loaded. Exiting.");
            return;
        }

        Console.WriteLine($"\nLoaded {stocks.Count} stocks.\n");

        // Menu loop
        bool running = true;
        while (running)
        {
            Console.WriteLine("\nSelect an option:");
            Console.WriteLine("1. One stock against 3 closest stocks");
            Console.WriteLine("2. One stock against another stock");
            Console.WriteLine("3. Quit");
            Console.Write("Choice: ");
            string choice = Console.ReadLine().Trim();

            switch (choice)
            {
                case "1":
                    OneStockTop3(stocks);
                    break;
                case "2":
                    OneStockVsAnother(stocks);
                    break;
                case "3":
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Try again.");
                    break;
            }
        }

        Console.WriteLine("Goodbye!");
    }

    static void OneStockTop3(List<StockData> stocks)
    {
        var stock = GetStockInput(stocks, "Enter the ticker of the target stock: ");
        if (stock == null) return;

        var top3 = stocks
            .Where(s => s.Ticker != stock.Ticker)
            .Select(s => new { s.Ticker, Score = Utils.PearsonCorrelation(stock.Returns, s.Returns) })
            .OrderByDescending(x => x.Score)
            .Take(3);

        Console.WriteLine($"\nTop 3 stocks correlated with {stock.Ticker}:");
        foreach (var s in top3)
            Console.WriteLine($"{s.Ticker} → correlation: {s.Score:P2}");
    }

    static void OneStockVsAnother(List<StockData> stocks)
    {
        var stock1 = GetStockInput(stocks, "Enter the ticker of the first stock: ");
        if (stock1 == null) return;

        var stock2 = GetStockInput(stocks, "Enter the ticker of the second stock: ");
        if (stock2 == null) return;

        double closeness = Utils.PearsonCorrelation(stock1.Returns, stock2.Returns);
        Console.WriteLine($"\nCloseness between {stock1.Ticker} and {stock2.Ticker}: {closeness:P2}");
    }

    static StockData GetStockInput(List<StockData> stocks, string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine().Trim().ToUpper();

            var stock = stocks.FirstOrDefault(s => s.Ticker.ToUpper() == input);
            if (stock != null)
                return stock;

            Console.WriteLine($"Ticker '{input}' not found. Please try again.");
        }
    }
}
