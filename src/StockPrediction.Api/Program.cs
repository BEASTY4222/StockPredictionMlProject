using StockPrediction.Api.Services;
using StockPrediction.ML.Services;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Get Supabase credentials from environment variables (safer than hardcoding)
var supabaseUrl = builder.Configuration["Supabase:Url"] 
    ?? throw new InvalidOperationException("Supabase:Url is not configured.");
var supabaseKey = builder.Configuration["Supabase:Key"] 
    ?? throw new InvalidOperationException("Supabase:Key is not configured.");
var alphaVantageKey = builder.Configuration["AlphaVantage:Key"] 
    ?? Environment.GetEnvironmentVariable("AlphaVantage:Key")
    ?? throw new InvalidOperationException("Alpha Vantage Key is not configured.");

// Register SupabaseService as a singleton (one instance for the whole app)
builder.Services.AddSingleton(new SupabaseService(supabaseUrl, supabaseKey));

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddSingleton(new AlphaVantageService(alphaVantageKey));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();