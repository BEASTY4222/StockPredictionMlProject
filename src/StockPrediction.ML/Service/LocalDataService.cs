using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StockPrediction.ML.Models;

namespace StockPrediction.ML.Services;

public class LocalDataService
{
    public List<StockData> LoadFromKaggleDataset(string symbol, string datasetPath = "Stocks")
    {
        // Ensure the dataset path exists
        if (!Directory.Exists(datasetPath))
            throw new DirectoryNotFoundException($"Dataset folder not found: {datasetPath}");

        // Try to find the file with case-insensitive search
        var files = Directory.GetFiles(datasetPath, "*.us.txt");
        var matchingFile = files.FirstOrDefault(f => 
            Path.GetFileNameWithoutExtension(f).Replace(".us", "", StringComparison.OrdinalIgnoreCase)
                .Equals(symbol, StringComparison.OrdinalIgnoreCase)
        );

        if (matchingFile == null)
        {
            // Try the exact naming convention
            var possibleFiles = new[]
            {
                Path.Combine(datasetPath, $"{symbol.ToLower()}.us.txt"),
                Path.Combine(datasetPath, $"{symbol.ToUpper()}.us.txt"),
                Path.Combine(datasetPath, $"{symbol}.us.txt"),
            };

            foreach (var file in possibleFiles)
            {
                if (File.Exists(file))
                {
                    matchingFile = file;
                    break;
                }
            }
        }

        if (matchingFile == null)
        {
            var availableFiles = string.Join(", ", files.Select(f => Path.GetFileName(f)).Take(10));
            throw new FileNotFoundException(
                $"Stock data file not found for symbol '{symbol}'. " +
                $"Available files (first 10): {availableFiles}. " +
                $"Searched in: {datasetPath}"
            );
        }

        Console.WriteLine($"Loading data from: {matchingFile}");

        var lines = File.ReadAllLines(matchingFile);
        var result = new List<StockData>();

        // Skip header
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            
            var parts = lines[i].Split(',');
            
            // Format: Date,Open,High,Low,Close,Volume,OpenInt
            // We need at least 6 columns
            if (parts.Length < 6) continue;

            try
            {
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
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Could not parse row {i}: {ex.Message}");
            }
        }

        if (result.Count == 0)
            throw new Exception($"No valid data rows found in {matchingFile}");

        Console.WriteLine($"Loaded {result.Count} rows for {symbol} from {matchingFile}");
        return result.OrderBy(d => d.Date).ToList();
    }
}