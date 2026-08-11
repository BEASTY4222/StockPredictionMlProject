using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Supabase;
using StockPrediction.Api.Models;

namespace StockPrediction.Api.Services;

public class SupabaseService
{
    private readonly Client _supabaseClient;

    public SupabaseService(string supabaseUrl, string supabaseKey)
    {
        var options = new Supabase.SupabaseOptions
        {
            AutoConnectRealtime = true // Enables real-time updates if needed
        };

        _supabaseClient = new Client(supabaseUrl, supabaseKey, options);
        
        // Initialize the client asynchronously
        _ = _supabaseClient.InitializeAsync();
    }

    /// <summary>
    /// Saves a new prediction record to the Supabase database.
    /// </summary>
    public async Task<PredictionRecord> SavePredictionAsync(PredictionRecord record)
    {
        var response = await _supabaseClient
            .From<PredictionRecord>()
            .Insert(record);

        // The response contains the inserted record with the auto-generated ID
        return response.Models[0];
    }

    /// <summary>
    /// Retrieves all predictions for a given stock symbol, ordered by creation date.
    /// </summary>
    public async Task<List<PredictionRecord>> GetPredictionsBySymbolAsync(string symbol)
    {
        var response = await _supabaseClient
            .From<PredictionRecord>()
            .Where(p => p.Symbol == symbol)
            .Order(p => p.CreatedAt, Supabase.Postgrest.Constants.Ordering.Ascending)
            .Get();

        return response.Models;
    }

    /// <summary>
    /// Retrieves the most recent prediction for a given stock symbol.
    /// </summary>
    public async Task<PredictionRecord?> GetLatestPredictionAsync(string symbol)
    {
        var response = await _supabaseClient
            .From<PredictionRecord>()
            .Where(p => p.Symbol == symbol)
            .Order(p => p.CreatedAt, Supabase.Postgrest.Constants.Ordering.Descending)
            .Limit(1)
            .Get();

        return response.Models.Count > 0 ? response.Models[0] : null;
    }
}