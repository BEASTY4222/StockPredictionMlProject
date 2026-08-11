using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using StockPrediction.Api.Models;
using StockPrediction.Api.Services;
using StockPrediction.ML;
using StockPrediction.ML.Models;
using StockPrediction.ML.Services;  

namespace StockPrediction.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PredictionController : ControllerBase
{
    private readonly SupabaseService _supabaseService;
    private readonly AlphaVantageService _alphaVantageService;

    public PredictionController(SupabaseService supabaseService, AlphaVantageService alphaVantageService)
    {
        _supabaseService = supabaseService;
        _alphaVantageService = alphaVantageService;
    }

    /// <summary>
    /// Generates a new prediction for a given stock symbol.
    /// Fetches historical data, trains the model, and saves the result to Supabase.
    /// </summary>
    [HttpPost("generate/{symbol}")]
    public async Task<IActionResult> GeneratePrediction(string symbol)
    {
        try
        {
            // 1. Fetch historical data (last 2 years)
            var endDate = DateTime.Now;
            var startDate = endDate.AddYears(-2);
            var historicalData = await _alphaVantageService.FetchHistoricalDataAsync(symbol);
            
            if (historicalData == null || historicalData.Count < 30)
                return BadRequest($"Not enough historical data for {symbol}. Need at least 30 days.");

            // 2. Train the ML model
            var predictor = new StockPricePredictor();
            predictor.Train(historicalData);

            // 3. Generate the forecast
            var predictionResult = predictor.Predict();

            // 4. Save each forecasted day to Supabase
            var savedRecords = new List<PredictionRecord>();
            for (int i = 0; i < predictionResult.ForecastedPrices.Length; i++)
            {
                var record = new PredictionRecord
                {
                    Symbol = symbol.ToUpper(),
                    PredictionDate = DateTime.Now.AddDays(i + 1).Date, // Tomorrow, then next day, etc.
                    PredictedPrice = (decimal)predictionResult.ForecastedPrices[i],
                    LowerBound = predictionResult.ConfidenceLower?.Length > i 
                        ? (decimal?)predictionResult.ConfidenceLower[i] 
                        : null,
                    UpperBound = predictionResult.ConfidenceUpper?.Length > i 
                        ? (decimal?)predictionResult.ConfidenceUpper[i] 
                        : null,
                    CreatedAt = DateTime.UtcNow
                };

                var saved = await _supabaseService.SavePredictionAsync(record);
                savedRecords.Add(saved);
            }

            return Ok(new
            {
                Symbol = symbol.ToUpper(),
                Predictions = savedRecords,
                Message = $"Successfully generated and saved {savedRecords.Count} predictions."
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    /// <summary>
    /// Retrieves all saved predictions for a given symbol.
    /// </summary>
    [HttpGet("history/{symbol}")]
    public async Task<IActionResult> GetPredictionHistory(string symbol)
    {
        var predictions = await _supabaseService.GetPredictionsBySymbolAsync(symbol.ToUpper());

        if (predictions == null || predictions.Count == 0)
            return NotFound($"No predictions found for symbol {symbol}.");

        return Ok(predictions);
    }

    /// <summary>
    /// Retrieves the most recent prediction for a given symbol.
    /// </summary>
    [HttpGet("latest/{symbol}")]
    public async Task<IActionResult> GetLatestPrediction(string symbol)
    {
        var prediction = await _supabaseService.GetLatestPredictionAsync(symbol.ToUpper());

        if (prediction == null)
            return NotFound($"No predictions found for symbol {symbol}.");

        return Ok(prediction);
    }
}