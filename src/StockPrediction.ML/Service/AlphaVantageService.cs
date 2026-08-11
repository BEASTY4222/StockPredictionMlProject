using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using StockPrediction.ML.Models;

namespace StockPrediction.ML.Services;

public class AlphaVantageService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public AlphaVantageService(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
    }

    public async Task<List<StockData>> FetchHistoricalDataAsync(string symbol)
    {
        try
        {
            // Alpha Vantage API URL for daily adjusted data
            var url = $"https://www.alphavantage.co/query?function=TIME_SERIES_DAILY_ADJUSTED&symbol={symbol}&apikey={_apiKey}&outputsize=compact";

            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var data = JObject.Parse(json);

            // Check for error messages
            if (data["Error Message"] != null)
                throw new Exception($"Alpha Vantage error: {data["Error Message"]}");

            if (data["Note"] != null)
                throw new Exception($"Alpha Vantage rate limit: {data["Note"]}");

            var timeSeries = data["Time Series (Daily)"];
            if (timeSeries == null)
                throw new Exception("No time series data found.");

            var result = new List<StockData>();

            foreach (var item in timeSeries.Children<JProperty>())
            {
                var date = DateTime.Parse(item.Name);
                var values = item.Value;

                result.Add(new StockData
                {
                    Date = date,
                    Open = decimal.Parse(values["1. open"].ToString()),
                    High = decimal.Parse(values["2. high"].ToString()),
                    Low = decimal.Parse(values["3. low"].ToString()),
                    Close = decimal.Parse(values["4. close"].ToString()),
                    Volume = long.Parse(values["6. volume"].ToString())
                });
            }

            // Return sorted by date (oldest first)
            result.Sort((a, b) => a.Date.CompareTo(b.Date));
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching data for {symbol}: {ex.Message}");
            throw;
        }
    }
}