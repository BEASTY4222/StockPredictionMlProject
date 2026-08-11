using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace StockPrediction.Web.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;

    public ApiService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PredictionResponse> GeneratePrediction(string symbol)
    {
        var response = await _httpClient.PostAsync($"api/Prediction/generate/{symbol.ToLower()}", null);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PredictionResponse>();
    }

    public async Task<List<PredictionDto>> GetPredictionHistory(string symbol)
    {
        return await _httpClient.GetFromJsonAsync<List<PredictionDto>>($"api/Prediction/history/{symbol.ToLower()}");
    }

    public async Task<PredictionDto> GetLatestPrediction(string symbol)
    {
        return await _httpClient.GetFromJsonAsync<PredictionDto>($"api/Prediction/latest/{symbol.ToLower()}");
    }
}

public class PredictionResponse
{
    public string Symbol { get; set; }
    public List<PredictionDto> Predictions { get; set; }
    public string Message { get; set; }
    public int DataPointsUsed { get; set; }
}

public class PredictionDto
{
    public int Id { get; set; }
    public string Symbol { get; set; }
    public DateTime PredictionDate { get; set; }
    public decimal PredictedPrice { get; set; }
    public decimal? LowerBound { get; set; }
    public decimal? UpperBound { get; set; }
    public DateTime CreatedAt { get; set; }
}