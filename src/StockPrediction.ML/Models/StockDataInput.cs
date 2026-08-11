using Microsoft.ML.Data;

namespace StockPrediction.ML.Models;

/// <summary>
/// ML.NET-friendly input model for stock data.
/// Uses float instead of decimal for compatibility with ML.NET.
/// </summary>
public class StockDataInput
{
    [LoadColumn(0)]
    public DateTime Date { get; set; }

    [LoadColumn(1)]
    public float Open { get; set; }

    [LoadColumn(2)]
    public float High { get; set; }

    [LoadColumn(3)]
    public float Low { get; set; }

    [LoadColumn(4)]
    public float Close { get; set; }

    [LoadColumn(5)]
    public float Volume { get; set; }
}