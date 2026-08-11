using System;
using System.Collections.Generic;
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

    // Constructor now receives both services via dependency injection
    public PredictionController(
        SupabaseService supabaseService,
        AlphaVantageService alphaVantageService)
    {
        _supabaseService = supabaseService;
        _alphaVantageService = alphaVantageService;
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { Message = "Pong! The API is working." });
    }

   [HttpPost("generate/{symbol}")]
    public async Task<IActionResult> GeneratePrediction(string symbol)
    {
        try
        {
            var localService = new LocalDataService();
            var datasetPath = Path.Combine(Directory.GetCurrentDirectory(), "Stocks");
            var historicalData = localService.LoadFromKaggleDataset(symbol, datasetPath);

            if (historicalData == null || historicalData.Count < 30)
                return BadRequest($"Not enough historical data for {symbol}. Found {historicalData?.Count ?? 0} rows.");

            var predictor = new StockPricePredictor();
            predictor.Train(historicalData);
            var predictionResult = predictor.Predict();

            var savedRecords = new List<PredictionDto>();
            for (int i = 0; i < predictionResult.ForecastedPrices.Length; i++)
            {
                var record = new PredictionRecord
                {
                    Symbol = symbol.ToLower(),
                    PredictionDate = DateTime.Now.AddDays(i + 1).Date,
                    PredictedPrice = (decimal)predictionResult.ForecastedPrices[i],
                    LowerBound = predictionResult.ConfidenceLower?.Length > i 
                        ? (decimal?)predictionResult.ConfidenceLower[i] 
                        : null,
                    UpperBound = predictionResult.ConfidenceUpper?.Length > i 
                        ? (decimal?)predictionResult.ConfidenceUpper[i] 
                        : null,
                    CreatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"Saving prediction {i+1}: {record.PredictedPrice}");
                var saved = await _supabaseService.SavePredictionAsync(record);
                
                savedRecords.Add(new PredictionDto
                {
                    Id = saved.Id,
                    Symbol = saved.Symbol,
                    PredictionDate = saved.PredictionDate,
                    PredictedPrice = saved.PredictedPrice,
                    LowerBound = saved.LowerBound,
                    UpperBound = saved.UpperBound,
                    CreatedAt = saved.CreatedAt
                });
            }

            return Ok(new
            {
                Symbol = symbol.ToLower(),
                Predictions = savedRecords,
                Message = $"Successfully generated and saved {savedRecords.Count} predictions.",
                DataPointsUsed = historicalData.Count
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== ERROR in GeneratePrediction ===");
            Console.WriteLine($"Symbol: {symbol}");
            Console.WriteLine($"Exception Type: {ex.GetType().FullName}");
            Console.WriteLine($"Message: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                Console.WriteLine($"Inner Stack Trace: {ex.InnerException.StackTrace}");
            }
            return StatusCode(500, $"An error occurred: {ex.Message}");
        }
    }

    [HttpGet("history/{symbol}")]
    public async Task<IActionResult> GetPredictionHistory(string symbol)
    {
        var predictions = await _supabaseService.GetPredictionsBySymbolAsync(symbol.ToLower());

        if (predictions == null || predictions.Count == 0)
            return NotFound($"No predictions found for symbol {symbol}.");

        // Map to DTOs
        var dtos = predictions.Select(p => new PredictionDto
        {
            Id = p.Id,
            Symbol = p.Symbol,
            PredictionDate = p.PredictionDate,
            PredictedPrice = p.PredictedPrice,
            LowerBound = p.LowerBound,
            UpperBound = p.UpperBound,
            CreatedAt = p.CreatedAt
        }).ToList();

        return Ok(dtos);
    }

    [HttpGet("latest/{symbol}")]
    public async Task<IActionResult> GetLatestPrediction(string symbol)
    {
        var prediction = await _supabaseService.GetLatestPredictionAsync(symbol.ToLower());

        if (prediction == null)
            return NotFound($"No predictions found for symbol {symbol}.");

        // Map to DTO
        var dto = new PredictionDto
        {
            Id = prediction.Id,
            Symbol = prediction.Symbol,
            PredictionDate = prediction.PredictionDate,
            PredictedPrice = prediction.PredictedPrice,
            LowerBound = prediction.LowerBound,
            UpperBound = prediction.UpperBound,
            CreatedAt = prediction.CreatedAt
        };

        return Ok(dto);
    }
}