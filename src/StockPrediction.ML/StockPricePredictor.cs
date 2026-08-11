using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms.TimeSeries;
using StockPrediction.ML.Models;

namespace StockPrediction.ML;

public class StockPricePredictor
{
    // The MLContext is the "kitchen" where all ML operations happen.
    private readonly MLContext _mlContext = new MLContext();

    // The trained model. Null until you call Train().
    private ITransformer? _trainedModel;

    // The number of days ahead we want to predict.
    private const int ForecastHorizon = 1;

    /// <summary>
    /// Trains the time-series forecasting model on historical stock data.
    /// </summary>
    /// <param name="historicalData">List of StockData objects (must be sorted by date ascending)</param>
    public void Train(List<StockData> historicalData)
    {
        // 1. Validate input
        if (historicalData == null || historicalData.Count < 30)
            throw new ArgumentException("You need at least 30 days of historical data to train a meaningful model.");

        // 2. Convert our List<StockData> into ML.NET's special data format: IDataView
        IDataView dataView = _mlContext.Data.LoadFromEnumerable(historicalData);

        // 3. Define the forecasting pipeline
        var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
            outputColumnName: nameof(StockPredictionResult.ForecastedPrices),
            inputColumnName: nameof(StockData.Close),
            windowSize: 7,              // Use the last 7 days to predict the next day
            seriesLength: 30,            // Look back 30 days to establish the pattern
            trainSize: historicalData.Count, // Use ALL available data for training
            horizon: ForecastHorizon,    // Predict N days into the future
            confidenceLevel: 0.95f,      // 95% confidence interval
            confidenceLowerBoundColumn: nameof(StockPredictionResult.ConfidenceLower),
            confidenceUpperBoundColumn: nameof(StockPredictionResult.ConfidenceUpper)
        );

        // 4. Train the model!
        _trainedModel = forecastingPipeline.Fit(dataView);
    }

    /// <summary>
    /// Generates a forecast for the next N days based on the trained model.
    /// </summary>
    public StockPredictionResult Predict()
    {
        if (_trainedModel == null)
            throw new InvalidOperationException("You must call Train() first before making predictions.");

        // 5. Create a "prediction engine" from the trained model
        var forecastEngine = _trainedModel.CreateTimeSeriesEngine<StockData, StockPredictionResult>(_mlContext);

        // 6. Generate the forecast
        var prediction = forecastEngine.Predict();

        return prediction;
    }

    /// <summary>
    /// Saves the trained model to a .zip file so you don't have to retrain every time.
    /// </summary>
    public void SaveModel(string filePath)
    {
        if (_trainedModel == null)
            throw new InvalidOperationException("No trained model to save.");

        _mlContext.Model.Save(_trainedModel, null, filePath);
    }

    /// <summary>
    /// Loads a previously saved model from a .zip file.
    /// </summary>
    public void LoadModel(string filePath)
    {
        _trainedModel = _mlContext.Model.Load(filePath, out _);
    }
}

// ================================================================
// DATA MODELS FOR ML.NET (These are separate from StockData)
// ================================================================

/// <summary>
/// The input data that ML.NET expects: just the Close price.
/// Note: ML.NET's time-series algorithms are UNIVARIATE – they ONLY use one column.
/// </summary>
public class StockDataInput
{
    public float Close { get; set; }
}

/// <summary>
/// The output that ML.NET generates after forecasting.
/// </summary>
public class StockPredictionResult
{
    // The array of predicted prices for the next N days (ForecastHorizon).
    public float[] ForecastedPrices { get; set; } = Array.Empty<float>();

    // The lower bound of the 95% confidence interval (array length = ForecastHorizon).
    public float[] ConfidenceLower { get; set; } = Array.Empty<float>();

    // The upper bound of the 95% confidence interval (array length = ForecastHorizon).
    public float[] ConfidenceUpper { get; set; } = Array.Empty<float>();
}