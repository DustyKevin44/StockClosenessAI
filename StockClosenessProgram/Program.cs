using System;
using System.Collections.Generic;
using System.Linq;

// NOTE: Assumes StockData, CsvLoader, CompanyInfo, CompanyDescriptions, Utils exist
namespace StockClosenessAI;
class Program
{
    static void Main()
    {
        string folderPath = "Data";
        int maxDays = 30;

        var stocks = CsvLoader.LoadAllStocksFromFolder(folderPath, maxDays);

        if (!stocks.Any())
        {
            Console.WriteLine("No stocks loaded. Exiting.");
            return;
        }

        Console.WriteLine($"\nLoaded {stocks.Count} stocks.\n");

        bool running = true;
        while (running)
        {
            Console.ReadKey();
            Console.Clear();
            Console.WriteLine("\nSelect an option:");
            Console.WriteLine("1. One stock against 3 closest stocks (cointegration only)");
            Console.WriteLine("2. One stock against another stock");
            Console.WriteLine("3. One stock against 3 most opposite stocks (non-cointegrated)");
            Console.WriteLine("4. Quit");
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

    static StockData GetStockInput(List<StockData> stocks, string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine().Trim().ToUpper();

            var stock = stocks.FirstOrDefault(s => s.Ticker.ToUpper() == input);
            if (stock != null) return stock;

            CompanyInfo info = CompanyDescriptions.Get(input);
            if (info != null)
            {
                Console.WriteLine($"'{input}' is defined ({info.Industry}), but no stock data was loaded for it.");
            }
            else
            {
                Console.WriteLine($"Ticker '{input}' not found. Please try again.");
            }
        }
    }

    static string GetOneLineDescription(string ticker, CompanyInfo info, double pearsonScore, double? cointegrationScore = null)
    {
        string name = "N/A Name";
        string industry = "N/A Industry";
        string descriptionSnippet = "N/A Description";

        string pearsonText = double.IsNaN(pearsonScore) ? "N/A" : pearsonScore.ToString("F4");
        string cointegrationText = cointegrationScore.HasValue ? $"{(cointegrationScore.Value * 100):F1}%" : "N/A";

        if (info != null)
        {
            industry = info.Industry;
            string fullDesc = info.Description;
            int lastParenStart = fullDesc.LastIndexOf('(');
            int lastParenEnd = fullDesc.LastIndexOf(')');
            if (lastParenStart != -1 && lastParenEnd != -1 && lastParenEnd > lastParenStart)
            {
                name = fullDesc.Substring(lastParenStart + 1, lastParenEnd - lastParenStart - 1).Trim();
                descriptionSnippet = fullDesc.Substring(0, lastParenStart).Trim().TrimEnd(',');
            }
            else
            {
                descriptionSnippet = fullDesc.Length > 50 ? fullDesc.Substring(0, 47) + "..." : fullDesc;
                name = info.Description;
            }
        }

        return $"{ticker} ({industry}) - {name} | Pearson: {pearsonText} | Cointegration: {cointegrationText} | {descriptionSnippet}";
    }

    static void OneStockTop3(List<StockData> stocks)
    {
        var stock = GetStockInput(stocks, "Enter the ticker of the target stock: ");
        if (stock == null) return;

        var top3 = stocks
            .Where(s => s.Ticker != stock.Ticker)
            .Select(s =>
            {
                double pearson = Utils.PearsonCorrelation(stock.Returns, s.Returns); // insight only
                double cointegration = Utils.CointegrationScore(stock.Returns, s.Returns);
                return new { s.Ticker, Pearson = pearson, Cointegration = cointegration };
            })
            .Where(x => x.Cointegration >= 0.5)      // threshold for reasonably strong cointegration
            .OrderByDescending(x => x.Cointegration) // sort by closeness
            .Take(3);

        Console.WriteLine($"\nTop 3 stocks most closely cointegrated with {stock.Ticker}:");

        foreach (var s in top3)
        {
            CompanyInfo info = CompanyDescriptions.Get(s.Ticker);
            Console.WriteLine(GetOneLineDescription(s.Ticker, info, s.Pearson, s.Cointegration));
        }

        if (!top3.Any())
        {
            Console.WriteLine("No cointegrated stocks found.");
        }
    }

    static void OneStockVsAnother(List<StockData> stocks)
    {
        var stock1 = GetStockInput(stocks, "Enter the ticker of the first stock: ");
        if (stock1 == null) return;
        var stock2 = GetStockInput(stocks, "Enter the ticker of the second stock: ");
        if (stock2 == null) return;

        double pearson = Utils.PearsonCorrelation(stock1.Returns, stock2.Returns);
        double coin = Utils.CointegrationScore(stock1.Returns, stock2.Returns);

        CompanyInfo info1 = CompanyDescriptions.Get(stock1.Ticker);
        CompanyInfo info2 = CompanyDescriptions.Get(stock2.Ticker);

        Console.WriteLine($"\n--- Stock 1 Details ---");
        Console.WriteLine(GetOneLineDescription(stock1.Ticker, info1, pearson, coin));

        Console.WriteLine($"\n--- Stock 2 Details ---");
        Console.WriteLine(GetOneLineDescription(stock2.Ticker, info2, pearson, coin));
    }

    static void OneStockMostOpposite(List<StockData> stocks)
    {
        var stock = GetStockInput(stocks, "Enter the ticker of the target stock: ");
        if (stock == null) return;

        var topOpposite = stocks
            .Where(s => s.Ticker != stock.Ticker)
            .Select(s =>
            {
                double pearson = Utils.PearsonCorrelation(stock.Returns, s.Returns); // insight only
                double cointegration = Utils.CointegrationScore(stock.Returns, s.Returns);
                return new { s.Ticker, Pearson = pearson, Cointegration = cointegration };
            })
            .Where(x => x.Cointegration < 0.2)     // weak / opposite
            .OrderBy(x => x.Cointegration)         // lowest first
            .Take(3);

        Console.WriteLine($"\nTop 3 stocks least likely to be cointegrated with {stock.Ticker}:");

        foreach (var s in topOpposite)
        {
            CompanyInfo info = CompanyDescriptions.Get(s.Ticker);
            Console.WriteLine(GetOneLineDescription(s.Ticker, info, s.Pearson, s.Cointegration));
        }

        if (!topOpposite.Any())
        {
            Console.WriteLine("No non-cointegrated stocks found.");
        }
    }
}
