using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Rinha.Fraud.Endpoints;

internal static class ReadyEndpoint
{
    public static IEndpointRouteBuilder MapReady(this IEndpointRouteBuilder app)
    {
        app.MapGet("/ready", () => Results.Ok());
        return app;
    }
}
