using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rinha.Fraud.Dataset;
using Rinha.Fraud.Endpoints;
using Rinha.Fraud.Hosting;
using Rinha.Fraud.Json;
using Rinha.Fraud.Scoring;
using Rinha.Fraud.Search;
using Rinha.Fraud.Vectorization;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Logging.ClearProviders();

builder.WebHost.ConfigureKestrel(KestrelListenerConfig.Configure);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, FraudJsonContext.Default);
    options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
});

builder.Services.AddSingleton(_ => ReferenceLoader.Load(
    ResourcePaths.ReferencesPath(),
    ResourcePaths.ResolveReferenceLimit(),
    ResourcePaths.ResolveL2Normalize()));
builder.Services.AddSingleton(_ => new MccRiskTable(StaticResources.LoadMccRisk(ResourcePaths.McCRiskPath())));
builder.Services.AddSingleton<Vectorizer>();
builder.Services.AddSingleton<IVectorIndex>(sp =>
    IndexBackendFactory.Create(sp.GetRequiredService<ReferenceDataset>()));
builder.Services.AddSingleton(sp => new FraudScorer(
    sp.GetRequiredService<Vectorizer>(),
    sp.GetRequiredService<IVectorIndex>(),
    sp.GetRequiredService<ReferenceDataset>(),
    ResourcePaths.ResolveL2Normalize()));

var app = builder.Build();

Warmup.Run(app.Services.GetRequiredService<FraudScorer>());

app.MapReady();
app.MapFraudScore();

app.Run();
