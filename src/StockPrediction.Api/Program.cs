using StockPrediction.Api.Services;
using StockPrediction.ML.Services;

using System;

Environment.SetEnvironmentVariable("COMPlus_EnableHWIntrinsic", "0");
Environment.SetEnvironmentVariable("DOTNET_EnableGPU", "0");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Read Supabase configuration
var supabaseUrl = builder.Configuration["Supabase:Url"] 
    ?? Environment.GetEnvironmentVariable("SUPABASE_URL")
    ?? throw new InvalidOperationException("Supabase URL is not configured.");
var supabaseKey = builder.Configuration["Supabase:Key"] 
    ?? Environment.GetEnvironmentVariable("SUPABASE_KEY")
    ?? throw new InvalidOperationException("Supabase Key is not configured.");

// Read Alpha Vantage configuration
var alphaVantageKey = builder.Configuration["AlphaVantage:Key"] 
    ?? Environment.GetEnvironmentVariable("ALPHA_VANTAGE_KEY")
    ?? throw new InvalidOperationException("Alpha Vantage Key is not configured.");

// Register services
builder.Services.AddSingleton(new SupabaseService(supabaseUrl, supabaseKey));
builder.Services.AddSingleton(new AlphaVantageService(alphaVantageKey));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();