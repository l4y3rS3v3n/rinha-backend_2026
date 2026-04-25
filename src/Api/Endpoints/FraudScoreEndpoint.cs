using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Rinha.Fraud.Contracts;
using Rinha.Fraud.Scoring;

namespace Rinha.Fraud.Endpoints;

internal static class FraudScoreEndpoint
{
    private static readonly FraudResponse SafeFallback = new(Approved: true, FraudScore: 0d);

    public static IEndpointRouteBuilder MapFraudScore(this IEndpointRouteBuilder app)
    {
        app.MapPost("/fraud-score", (FraudRequest request, FraudScorer scorer) =>
        {
            try
            {
                return Results.Ok(scorer.Score(request));
            }
#pragma warning disable CA1031 // HTTP 5xx costs more than any mis-classification
            catch (Exception)
#pragma warning restore CA1031
            {
                return Results.Ok(SafeFallback);
            }
        });
        return app;
    }
}
