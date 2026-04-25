using Rinha.Fraud.Dataset;
using Rinha.Fraud.Vectorization;

namespace Rinha.Fraud.Search;

internal sealed class BruteForceIndex : IVectorIndex
{
    private readonly ReferenceDataset _dataset;

    public BruteForceIndex(ReferenceDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        _dataset = dataset;
    }

    public void Search(ReadOnlySpan<float> query, Span<int> topK)
    {
        const int dims = NormalizationConstants.VectorDimensions;
        if (query.Length != dims)
            throw new ArgumentException($"query must be {dims}-dim", nameof(query));
        if (topK.Length == 0)
            throw new ArgumentException("topK must be non-empty", nameof(topK));

        var refs = _dataset.Vectors;
        var count = _dataset.Count;
        var k = topK.Length;

        Span<float> bestDist = stackalloc float[k];
        Span<int> bestIdx = stackalloc int[k];
        for (var i = 0; i < k; i++)
        {
            bestDist[i] = float.PositiveInfinity;
            bestIdx[i] = -1;
        }

        for (var i = 0; i < count; i++)
        {
            var offset = i * dims;
            var dist = SquaredDistance(query, refs.Slice(offset, dims));

            if (dist >= bestDist[k - 1])
                continue;

            var pos = k - 1;
            while (pos > 0 && dist < bestDist[pos - 1])
            {
                bestDist[pos] = bestDist[pos - 1];
                bestIdx[pos] = bestIdx[pos - 1];
                pos--;
            }
            bestDist[pos] = dist;
            bestIdx[pos] = i;
        }

        bestIdx.CopyTo(topK);
    }

    private static float SquaredDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        var sum = 0f;
        for (var d = 0; d < a.Length; d++)
        {
            var diff = a[d] - b[d];
            sum += diff * diff;
        }
        return sum;
    }
}
