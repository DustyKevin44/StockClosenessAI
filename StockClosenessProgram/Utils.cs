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
        if (n < 2) return (new List<double>(), new List<double>());

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

        double sumXY = 0, sumX2 = 0, sumY2 = 0;

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

    /// <summary>
    /// Fit Y = alpha + beta*X and return residuals.
    /// </summary>
    public static (double alpha, double beta, List<double> residuals) GetResiduals(List<double> x, List<double> y)
    {
        if (x.Count != y.Count || x.Count < 2)
            return (0, 0, new List<double>());

        int n = x.Count;
        double meanX = x.Average();
        double meanY = y.Average();

        double numerator = 0;
        double denominator = 0;

        for (int i = 0; i < n; i++)
        {
            numerator += (x[i] - meanX) * (y[i] - meanY);
            denominator += (x[i] - meanX) * (x[i] - meanX);
        }

        if (denominator == 0)
            return (0, 0, new List<double>());

        double beta = numerator / denominator;
        double alpha = meanY - beta * meanX;

        var residuals = new List<double>(n);
        for (int i = 0; i < n; i++)
            residuals.Add(y[i] - (alpha + beta * x[i]));

        return (alpha, beta, residuals);
    }

    /// <summary>
    /// Very primitive mean-reversion check (not used in the new model).
    /// </summary>
    public static bool IsStationary(List<double> series, double varianceThreshold = 0.001)
    {
        if (series.Count < 2) return false;

        double mean = series.Average();
        double variance = series.Sum(v => (v - mean) * (v - mean)) / series.Count;

        return variance < varianceThreshold;
    }

    /// <summary>
    /// Returns cointegration strength from 0 (none) to 1 (strong).
    /// This uses residual variance as a simple proxy.
    /// </summary>
    public static double CointegrationScore(List<double> x, List<double> y)
    {
        var (_, _, residuals) = GetResiduals(x, y);
        if (residuals.Count < 2)
            return 0;

        double variance = residuals.Select(r => r * r).Average();

        if (double.IsNaN(variance) || variance <= 0)
            return 1.0;

        // Smooth mapping: score = 1 / (1 + variance)
        double score = 1.0 / (1.0 + variance);

        // Clamp to [0,1]
        return Math.Max(0, Math.Min(1, score));
    }

    /// <summary>
    /// Boolean shortcut for "strong enough cointegration".
    /// </summary>
    public static bool AreCointegrated(List<double> x, List<double> y)
    {
        return CointegrationScore(x, y) >= 0.5;  // recommended threshold
    }
}
