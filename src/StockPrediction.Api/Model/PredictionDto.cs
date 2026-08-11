using System;

namespace StockPrediction.Api.Models;

/// <summary>
/// DTO (Data Transfer Object) for predictions.
/// This is what the API returns to the client.
/// </summary>
public class PredictionDto
{
    public long Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public DateTime PredictionDate { get; set; }
    public decimal PredictedPrice { get; set; }
    public decimal? LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
    public DateTime CreatedAt { get; set; }
}