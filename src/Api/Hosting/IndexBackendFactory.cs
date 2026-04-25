using Rinha.Fraud.Dataset;
using Rinha.Fraud.Search;

namespace Rinha.Fraud.Hosting;

internal static class IndexBackendFactory
{
    public const string BackendSimd = "simd";
    public const string BackendHnsw = "hnsw";

    public static IVectorIndex Create(ReferenceDataset dataset)
    {
        var backend = (Environment.GetEnvironmentVariable(ResourcePaths.EnvIndexBackend)
            ?? BackendSimd).Trim().ToLowerInvariant();

        return backend switch
        {
            BackendHnsw => BuildHnsw(dataset),
            BackendSimd => new SimdBruteForceIndex(dataset),
            _ => throw new InvalidOperationException(
                $"Unknown {ResourcePaths.EnvIndexBackend}='{backend}'. Valid: simd, hnsw"),
        };
    }

    private static HnswIndex BuildHnsw(ReferenceDataset dataset)
    {
        var opts = new HnswOptions(
            M: ParseIntEnv(ResourcePaths.EnvHnswM, 16),
            MMax0: ParseIntEnv(ResourcePaths.EnvHnswM, 16) * 2,
            EfConstruction: ParseIntEnv(ResourcePaths.EnvHnswEfConstruction, 200),
            EfSearch: ParseIntEnv(ResourcePaths.EnvHnswEfSearch, 64));

        var graph = HnswBuilder.Build(dataset, opts);
        return new HnswIndex(dataset, graph, opts);
    }

    private static int ParseIntEnv(string name, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return int.TryParse(raw, out var parsed) && parsed > 0 ? parsed : fallback;
    }
}
