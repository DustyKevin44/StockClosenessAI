using System;
using System.Collections.Generic;
using System.Linq;

// NOTE: This file assumes the following classes/methods exist and are correct:
// - StockData class (with Ticker and Returns properties)
// - CsvLoader.LoadAllStocksFromFolder()
// - Utils.PearsonCorrelation(), Utils.CorrelationDescription()
// - CompanyInfo class (with Industry and Description properties)
// - CompanyDescriptions.Get()
namespace StockClosenessAI;
class Program
{
    static void Main()
    {
        string folderPath = "Data"; // Folder containing all CSVs
        int maxDays = 90;           // Last N days to use

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
            Console.Clear();
            Console.WriteLine("\nSelect an option:");
            Console.WriteLine("1. One stock against 3 closest stocks");
            Console.WriteLine("2. One stock against another stock");
            Console.WriteLine("3. One stock against 3 most opposite stocks");
            Console.WriteLine("4. Quit"); // Quit is now 4
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
                    OneStockMostOpposite(stocks);
                    break;
                case "4":
                    running = false;
                    break;
                default:
                    Console.WriteLine("Invalid choice. Try again.");
                    break;
            }
        }

        Console.WriteLine("Goodbye!");
    }
    
    // --- Helper Function: GetStockInput ---
    static StockData GetStockInput(List<StockData> stocks, string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine().Trim().ToUpper();

            var stock = stocks.FirstOrDefault(s => s.Ticker.ToUpper() == input);
            if (stock != null)
                return stock;

            CompanyInfo info = CompanyDescriptions.Get(input);
            if (info != null)
            {
                Console.WriteLine($"'{input}' is defined ({info.Industry}), but no stock data was loaded for it. Try a different ticker.");
            }
            else
            {
                Console.WriteLine($"Ticker '{input}' not found. Please try again.");
            }
        }
    }

    // --- Helper Function: GetOneLineDescription (NEW) ---
    static string GetOneLineDescription(string ticker, CompanyInfo info, double score)
    {
        string name = "N/A Name";
        string industry = "N/A Industry";
        string descriptionSnippet = "N/A Description";
        string closenessText = double.IsNaN(score) ? "N/A Score" : Utils.CorrelationDescription(score);
        string scoreDisplay = double.IsNaN(score) ? "N/A" : score.ToString("F4");

        if (info != null)
        {
            industry = info.Industry;
            string fullDesc = info.Description;
            int lastParenStart = fullDesc.LastIndexOf('(');
            int lastParenEnd = fullDesc.LastIndexOf(')');

            if (lastParenStart != -1 && lastParenEnd != -1 && lastParenEnd > lastParenStart)
            {
                // Extract the name (text inside the last parentheses)
                name = fullDesc.Substring(lastParenStart + 1, lastParenEnd - lastParenStart - 1).Trim();
                // Use the main part of the description as the snippet
                descriptionSnippet = fullDesc.Substring(0, lastParenStart).Trim().TrimEnd(',');
            }
            else
            {
                descriptionSnippet = fullDesc.Length > 50 ? fullDesc.Substring(0, 47) + "..." : fullDesc;
                name = info.Description;
            }
        }
        
        // Final One-Line Format: [Ticker] ([Industry]) - [Name]: [Closeness] ([Score]) | [Description snippet]
        return $"{ticker} ({industry}) - {name}: {closenessText} ({scoreDisplay}) | {descriptionSnippet}";
    }
    
    // --- Function 1: OneStockTop3 (Closest Stocks) ---
    static void OneStockTop3(List<StockData> stocks)
    {
        var stock = GetStockInput(stocks, "Enter the ticker of the target stock: ");
        if (stock == null) return;

        var top3 = stocks
            .Where(s => s.Ticker != stock.Ticker)
            .Select(s => new { s.Ticker, Score = Utils.PearsonCorrelation(stock.Returns, s.Returns) })
            .Where(x => !double.IsNaN(x.Score))
            .OrderByDescending(x => x.Score)
            .Take(3);

        Console.WriteLine($"\nTop 3 stocks most correlated with {stock.Ticker}:");
        foreach (var s in top3)
        {
            CompanyInfo info = CompanyDescriptions.Get(s.Ticker);
            Console.WriteLine(GetOneLineDescription(s.Ticker, info, s.Score));
        }
    }

    // --- Function 2: OneStockVsAnother ---
    static void OneStockVsAnother(List<StockData> stocks)
    {
        var stock1 = GetStockInput(stocks, "Enter the ticker of the first stock: ");
        if (stock1 == null) return;

        var stock2 = GetStockInput(stocks, "Enter the ticker of the second stock: ");
        if (stock2 == null) return;

        double closeness = Utils.PearsonCorrelation(stock1.Returns, stock2.Returns);
        
        CompanyInfo info1 = CompanyDescriptions.Get(stock1.Ticker);
        CompanyInfo info2 = CompanyDescriptions.Get(stock2.Ticker);
        
        // Output details for both stocks (score is NaN here, as it's the correlation score itself)
        Console.WriteLine($"\n--- Stock 1 Details ---");
        Console.WriteLine(GetOneLineDescription(stock1.Ticker, info1, double.NaN));
        
        Console.WriteLine($"\n--- Stock 2 Details ---");
        Console.WriteLine(GetOneLineDescription(stock2.Ticker, info2, double.NaN));

        Console.WriteLine($"\nOverall Closeness: {Utils.CorrelationDescription(closeness)} (Score: {closeness:F4})");
    }

    // --- Function 3: OneStockMostOpposite (Furthest Stocks) ---
    static void OneStockMostOpposite(List<StockData> stocks)
    {
        var stock = GetStockInput(stocks, "Enter the ticker of the target stock: ");
        if (stock == null) return;

        var topOpposite = stocks
            .Where(s => s.Ticker != stock.Ticker)
            .Select(s => new { s.Ticker, Score = Utils.PearsonCorrelation(stock.Returns, s.Returns) })
            .Where(x => !double.IsNaN(x.Score))
            .OrderBy(x => x.Score) // Furthest (most opposite) means lowest correlation (most negative)
            .Take(3);

        Console.WriteLine($"\nTop 3 stocks that move most opposite to {stock.Ticker}:");
        foreach (var s in topOpposite)
        {
            CompanyInfo info = CompanyDescriptions.Get(s.Ticker);
            Console.WriteLine(GetOneLineDescription(s.Ticker, info, s.Score));
        }
    }
}