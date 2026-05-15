using System.Text.Json;
using System.Text.Json.Serialization;
using Rinha.Core.Fraud;
using Rinha.Core.Models;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

var indexPath = Environment.GetEnvironmentVariable("INDEX_PATH") ?? "data/references.bin";
var indexLoaded = false;
var index = ReferenceIndex.Empty;

try
{
    if (File.Exists(indexPath))
    {
        index = ReferenceIndex.Load(indexPath);
        indexLoaded = index.Count > 0;
        Console.WriteLine($"Loaded reference index '{indexPath}' with {index.Count} vectors.");
    }
    else
    {
        Console.WriteLine($"Reference index not found at '{indexPath}'.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to load reference index '{indexPath}': {ex.Message}");
}

var scorer = new FraudScorer(index);
var jsonOptions = new JsonSerializerOptions
{
    PropertyNameCaseInsensitive = false
};

var app = builder.Build();

app.MapGet("/ready", () => indexLoaded ? Results.Ok() : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

app.MapPost("/fraud-score", async (HttpRequest httpRequest) =>
{
    try
    {
        var request = await JsonSerializer.DeserializeAsync<TransactionRequest>(httpRequest.Body, jsonOptions);
        if (request is null)
        {
            return Results.Json(FallbackDecision.Value);
        }

        var decision = scorer.Score(request);
        return Results.Json(new FraudScoreResponse(decision.Approved, decision.FraudScore));
    }
    catch
    {
        return Results.Json(FallbackDecision.Value);
    }
});

app.Run();

internal sealed record FraudScoreResponse(
    [property: JsonPropertyName("approved")] bool Approved,
    [property: JsonPropertyName("fraud_score")] float FraudScore);

internal static class FallbackDecision
{
    public static readonly FraudScoreResponse Value = new(true, 0.0f);
}
