using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rinha.Fraud.Dataset;

internal static class StaticResources
{
    public static Dictionary<string, float> LoadMccRisk(string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        using var fs = File.OpenRead(path);
        var raw = JsonSerializer.Deserialize(fs, StaticJsonContext.Default.DictionaryStringDouble)
            ?? throw new InvalidDataException($"Failed to load mcc_risk from {path}");

        var result = new Dictionary<string, float>(raw.Count, StringComparer.Ordinal);
        foreach (var kv in raw)
            result[kv.Key] = (float)kv.Value;
        return result;
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(Dictionary<string, double>))]
internal sealed partial class StaticJsonContext : JsonSerializerContext;
