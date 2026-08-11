using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace StockPrediction.Api.Models;

[Table("predictions")]
public class PredictionRecord : BaseModel
{
    [PrimaryKey("id", false)]
    public long Id { get; set; }

    [Column("symbol")]
    public string Symbol { get; set; } = string.Empty;

    [Column("prediction_date")]
    public DateTime PredictionDate { get; set; }

    [Column("predicted_price")]
    public decimal PredictedPrice { get; set; }

    [Column("lower_bound")]
    public decimal? LowerBound { get; set; }

    [Column("upper_bound")]
    public decimal? UpperBound { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }
}