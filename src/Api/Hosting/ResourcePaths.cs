namespace Rinha.Fraud.Hosting;

internal static class ResourcePaths
{
    public const string EnvDirectory = "RINHA_RESOURCES_DIR";
    public const string EnvListenSocket = "RINHA_LISTEN_SOCKET";
    public const string EnvListenPort = "RINHA_LISTEN_PORT";
    public const string EnvWarmupIterations = "RINHA_WARMUP_ITERATIONS";

    public const string EnvReferenceLimit = "RINHA_REFERENCE_LIMIT";

    public const string EnvIndexBackend = "RINHA_INDEX_BACKEND";
    public const string EnvHnswEfSearch = "RINHA_HNSW_EF_SEARCH";
    public const string EnvHnswEfConstruction = "RINHA_HNSW_EF_CONSTRUCTION";
    public const string EnvHnswM = "RINHA_HNSW_M";

    public const string EnvL2Normalize = "RINHA_L2_NORMALIZE";

    public static bool ResolveL2Normalize()
    {
        var raw = Environment.GetEnvironmentVariable(EnvL2Normalize);
        if (string.IsNullOrEmpty(raw)) return false;
        return raw.Equals("true", StringComparison.OrdinalIgnoreCase)
            || raw.Equals("1", StringComparison.Ordinal);
    }

    public const string DefaultDirectory = "/app/resources";
    public const string ReferencesFile = "references.json.gz";
    public const string McCRiskFile = "mcc_risk.json";

    public static string ResolveDirectory() =>
        Environment.GetEnvironmentVariable(EnvDirectory) ?? DefaultDirectory;

    public static string ReferencesPath() => Path.Combine(ResolveDirectory(), ReferencesFile);

    public static string McCRiskPath() => Path.Combine(ResolveDirectory(), McCRiskFile);

    public static int? ResolveReferenceLimit()
    {
        var raw = Environment.GetEnvironmentVariable(EnvReferenceLimit);
        if (string.IsNullOrEmpty(raw)) return null;
        return int.TryParse(raw, out var n) && n > 0 ? n : null;
    }
}
