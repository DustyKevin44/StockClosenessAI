using System;
using System.Collections.Generic;
using System.Linq;

public static class Utils
{
    /// <summary>
    /// Computes returns for a list of prices.
    /// Use logReturns = true for log returns (more stable), false for simple returns.
    /// </summary>
    public static List<double> ComputeReturns(List<decimal> prices, bool logReturns = true)
    {
        var returns = new List<double>();
        for (int i = 1; i < prices.Count; i++)
        {
            if (prices[i - 1] == 0) continue; // avoid divide by zero
            if (logReturns)
                returns.Add(Math.Log((double)prices[i] / (double)prices[i - 1]));
            else
                returns.Add(((double)prices[i] - (double)prices[i - 1]) / (double)prices[i - 1]);
        }
        return returns;
    }

    /// <summary>
    /// Aligns returns for two stocks by overlapping most recent days.
    /// Returns empty lists if overlap < 2 days.
    /// </summary>
    public static (List<double> X, List<double> Y) GetAlignedReturns(StockData s1, StockData s2)
    {
        int n = Math.Min(s1.Returns.Count, s2.Returns.Count);
        if (n < 2) return (new List<double>(), new List<double>()); // not enough overlap

        var x = s1.Returns.Skip(s1.Returns.Count - n).Take(n).ToList();
        var y = s2.Returns.Skip(s2.Returns.Count - n).Take(n).ToList();
        return (x, y);
    }

    /// <summary>
    /// Computes robust Pearson correlation between two lists of numbers.
    /// Returns 0 if lists are empty, different lengths, or have zero variance.
    /// </summary>
    public static double PearsonCorrelation(List<double> x, List<double> y)
    {
        if (x.Count == 0 || y.Count == 0 || x.Count != y.Count)
            return 0;

        int n = x.Count;
        double meanX = x.Average();
        double meanY = y.Average();

        double sumXY = 0;
        double sumX2 = 0;
        double sumY2 = 0;

        for (int i = 0; i < n; i++)
        {
            double dx = x[i] - meanX;
            double dy = y[i] - meanY;
            sumXY += dx * dy;
            sumX2 += dx * dx;
            sumY2 += dy * dy;
        }

        if (sumX2 == 0 || sumY2 == 0)
            return 0;

        return sumXY / Math.Sqrt(sumX2 * sumY2);
    }
    public static string CorrelationDescription(double corr)
        {
            corr = Math.Round(corr * 100, 0);
            string desc;

            if (corr >= 80) desc = "very strongly moves together";
            else if (corr >= 60) desc = "moves fairly often in the same direction";
            else if (corr >= 30) desc = "moves sometimes in the same direction";
            else if (corr >= 0) desc = "moves mostly independently";
            else if (corr >= -30) desc = "moves mostly independently or slightly opposite";
            else if (corr >= -60) desc = "moves fairly often in the opposite direction";
            else desc = "moves very strongly in the opposite direction";

            return $"{Math.Round(corr, 0)}% → {desc}";
        }
}
