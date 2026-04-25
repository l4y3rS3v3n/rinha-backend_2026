using System.Text.Json.Serialization;

namespace Rinha.Fraud.Dataset;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    GenerationMode = JsonSourceGenerationMode.Default)]
[JsonSerializable(typeof(RawReferenceRecord[]))]
[JsonSerializable(typeof(RawReferenceRecord))]
internal sealed partial class ReferenceJsonContext : JsonSerializerContext;
