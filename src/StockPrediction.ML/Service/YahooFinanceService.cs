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
        // 1. Call the Yahoo Finance library to download the raw data
        var historical = await Yahoo.GetHistoricalAsync(symbol, startDate, endDate, Period.Daily);

        // 2. Convert the raw Yahoo data into our own StockData model
        List<StockData> result = historical.Select(h => new StockData
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
}