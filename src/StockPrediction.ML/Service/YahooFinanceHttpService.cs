using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StockPrediction.ML.Models;

namespace StockPrediction.ML.Services;

public class YahooFinanceHttpService
{
    private readonly HttpClient _httpClient;
    private string _cookie;
    private string _crumb;

    public YahooFinanceHttpService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36");
    }

    private async Task RefreshTokenAsync(string symbol = "AAPL")
    {
        // Step 1: Visit Yahoo Finance to get a session cookie
        var quoteUrl = $"https://finance.yahoo.com/quote/{symbol}";
        var response = await _httpClient.GetAsync(quoteUrl);
        response.EnsureSuccessStatusCode();

        // Extract the cookie
        if (response.Headers.TryGetValues("Set-Cookie", out var cookieValues))
        {
            _cookie = cookieValues.FirstOrDefault()?.Split(';')[0];
        }

        // Step 2: Get the crumb value from the HTML
        var html = await response.Content.ReadAsStringAsync();
        var crumbMatch = System.Text.RegularExpressions.Regex.Match(html, "CrumbStore\":{\"crumb\":\"(?<crumb>\\w+)\"}");
        if (crumbMatch.Success)
        {
            _crumb = crumbMatch.Groups["crumb"].Value;
        }

        if (string.IsNullOrEmpty(_cookie) || string.IsNullOrEmpty(_crumb))
        {
            throw new Exception("Failed to obtain Yahoo Finance authentication token.");
        }

        // Add the cookie to the client for subsequent requests
        _httpClient.DefaultRequestHeaders.Add("Cookie", _cookie);
    }

    public async Task<List<StockData>> FetchHistoricalDataAsync(string symbol, DateTime startDate, DateTime endDate)
    {
        try
        {
            // Ensure we have a valid token
            await RefreshTokenAsync(symbol);

            // Build the request URL for the v7 API
            var startUnix = ((DateTimeOffset)startDate).ToUnixTimeSeconds();
            var endUnix = ((DateTimeOffset)endDate).ToUnixTimeSeconds();
            var url = $"https://query1.finance.yahoo.com/v7/finance/download/{symbol}?period1={startUnix}&period2={endUnix}&interval=1d&events=history&crumb={_crumb}";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var csv = await response.Content.ReadAsStringAsync();
            var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            var result = new List<StockData>();
            // Skip header line (index 0)
            for (int i = 1; i < lines.Length; i++)
            {
                var parts = lines[i].Split(',');
                if (parts.Length < 6) continue;

                result.Add(new StockData
                {
                    Date = DateTime.Parse(parts[0]),
                    Open = decimal.Parse(parts[1]),
                    High = decimal.Parse(parts[2]),
                    Low = decimal.Parse(parts[3]),
                    Close = decimal.Parse(parts[4]),
                    Volume = long.Parse(parts[5])
                });
            }

            return result.OrderBy(d => d.Date).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data for {symbol}: {ex.Message}");
            throw;
        }
    }
}