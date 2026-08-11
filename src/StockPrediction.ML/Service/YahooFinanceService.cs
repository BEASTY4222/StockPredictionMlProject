using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YahooFinanceApi;
using StockPrediction.ML.Models;

namespace StockPrediction.ML.Services;

public class YahooFinanceService
{
    public async Task<List<StockData>> FetchHistoricalDataAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate)
    {
        try
        {
            // Use the older GetHistorical method with explicit period
            var historical = await Yahoo.GetHistoricalAsync(
                symbol, 
                startDate, 
                endDate, 
                Period.Daily);

            var result = historical
                .Select(h => new StockData
                {
                    Date = h.DateTime,
                    Open = (decimal)h.Open,
                    High = (decimal)h.High,
                    Low = (decimal)h.Low,
                    Close = (decimal)h.Close,
                    Volume = h.Volume
                })
                .OrderBy(d => d.Date)
                .ToList();

            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data for {symbol}: {ex.Message}");
            throw;
        }
    }
}