using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Data;
using StockPrediction.ML.Models;

namespace StockPrediction.ML;

public class StockPricePredictor
{
    private readonly MLContext _mlContext = new MLContext();
    private ITransformer? _trainedModel;
    private int _lastIndex = 0;
    private float _lastClose = 0;
    private const int ForecastHorizon = 5;

    public void Train(List<StockData> historicalData)
    {
        if (historicalData == null || historicalData.Count < 30)
            throw new ArgumentException("Need at least 30 days of data.");

        // Create training data with lag features
        var trainingData = new List<StockDataInput>();
        for (int i = 3; i < historicalData.Count; i++)
        {
            trainingData.Add(new StockDataInput
            {
                DaysFromStart = i,
                Close = (float)historicalData[i].Close,
                Lag1 = (float)historicalData[i - 1].Close,
                Lag2 = (float)historicalData[i - 2].Close,
                Lag3 = (float)historicalData[i - 3].Close
            });
        }

        IDataView dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

        // Build pipeline: concatenate features, then train with Sdca (no MKL)
        var pipeline = _mlContext.Transforms.Concatenate("Features", 
                nameof(StockDataInput.Lag1),
                nameof(StockDataInput.Lag2),
                nameof(StockDataInput.Lag3))
            .Append(_mlContext.Regression.Trainers.Sdca(
                labelColumnName: nameof(StockDataInput.Close),
                maximumNumberOfIterations: 100));

        _trainedModel = pipeline.Fit(dataView);
        
        // Store last known values for prediction
        _lastClose = (float)historicalData.Last().Close;
        _lastIndex = historicalData.Count;
    }

    public StockPredictionResult Predict()
    {
        if (_trainedModel == null)
            throw new InvalidOperationException("Must call Train() first.");

        var engine = _mlContext.Model.CreatePredictionEngine<StockDataInput, StockPredictionOutput>(_trainedModel);
        
        // Predict next N days
        var predictions = new float[ForecastHorizon];
        float lag1 = _lastClose;
        float lag2 = _lastClose * 0.99f; // approximate
        float lag3 = _lastClose * 0.98f;

        for (int i = 0; i < ForecastHorizon; i++)
        {
            var input = new StockDataInput
            {
                DaysFromStart = _lastIndex + i + 1,
                Lag1 = lag1,
                Lag2 = lag2,
                Lag3 = lag3,
                Close = 0 // not used for prediction
            };

            var result = engine.Predict(input);
            predictions[i] = result.PredictedClose;

            // Shift lags for next iteration
            lag3 = lag2;
            lag2 = lag1;
            lag1 = predictions[i];
        }

        // Generate confidence intervals (simple approximation)
        var lower = predictions.Select(p => p * 0.96f).ToArray();
        var upper = predictions.Select(p => p * 1.04f).ToArray();

        return new StockPredictionResult
        {
            ForecastedPrices = predictions,
            ConfidenceLower = lower,
            ConfidenceUpper = upper
        };
    }
}

// Input model for ML.NET
public class StockDataInput
{
    public float DaysFromStart { get; set; }
    public float Close { get; set; } // label
    public float Lag1 { get; set; }
    public float Lag2 { get; set; }
    public float Lag3 { get; set; }
}

// Output model
public class StockPredictionOutput
{
    [ColumnName("Score")]
    public float PredictedClose { get; set; }
}
public class StockPredictionResult
{
    public float[] ForecastedPrices { get; set; } = Array.Empty<float>();
    public float[] ConfidenceLower { get; set; } = Array.Empty<float>();
    public float[] ConfidenceUpper { get; set; } = Array.Empty<float>();
}