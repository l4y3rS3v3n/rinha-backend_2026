using System.IO.Compression;
using System.Text.Json;
using Rinha.Fraud.Vectorization;

namespace Rinha.Fraud.Dataset;

internal static class ReferenceLoader
{
    private const string FraudLabel = "fraud";
    private const string LegitLabel = "legit";

    public static ReferenceDataset Load(string path, int? limit = null, bool l2Normalize = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(path);
        if (!File.Exists(path))
            throw new FileNotFoundException($"Reference dataset not found: {path}", path);

        using var fs = File.OpenRead(path);
        using Stream src = path.EndsWith(".gz", StringComparison.OrdinalIgnoreCase)
            ? new GZipStream(fs, CompressionMode.Decompress)
            : fs;

        var raw = JsonSerializer.Deserialize(src, ReferenceJsonContext.Default.RawReferenceRecordArray)
            ?? throw new InvalidDataException("Reference dataset deserialized to null");

        if (limit is int n && n < raw.Length)
            raw = raw.AsSpan(0, n).ToArray();

        return Build(raw, l2Normalize);
    }

    private static ReferenceDataset Build(RawReferenceRecord[] raw, bool l2Normalize)
    {
        const int dims = NormalizationConstants.VectorDimensions;

        var vectors = new float[raw.Length * dims];
        var labels = new Label[raw.Length];

        for (var i = 0; i < raw.Length; i++)
        {
            var rec = raw[i];
            if (rec.Vector.Length != dims)
                throw new InvalidDataException(
                    $"Reference record {i} has {rec.Vector.Length} dims, expected {dims}");

            var offset = i * dims;
            for (var d = 0; d < dims; d++)
                vectors[offset + d] = (float)rec.Vector[d];

            if (l2Normalize)
                Vectorization.L2Normalizer.NormalizeInPlace(vectors.AsSpan(offset, dims));

            labels[i] = rec.Label switch
            {
                FraudLabel => Label.Fraud,
                LegitLabel => Label.Legit,
                _ => throw new InvalidDataException($"Unknown label at {i}: {rec.Label}"),
            };
        }

        return new ReferenceDataset(vectors, labels);
    }
}
