using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Rinha.Fraud.Dataset;
using Rinha.Fraud.Vectorization;

namespace Rinha.Fraud.Search;

internal sealed class SimdBruteForceIndex : IVectorIndex
{
    private readonly ReferenceDataset _dataset;

    // 14-dim vector → 2 × 256-bit registers; last 2 lanes of the tail are masked.
    private static readonly Vector256<float> TailMask = Vector256.Create(
        -1f, -1f, -1f, -1f, -1f, -1f, 0f, 0f);

    public SimdBruteForceIndex(ReferenceDataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        if (!Avx2.IsSupported || !Fma.IsSupported)
            throw new PlatformNotSupportedException("SimdBruteForceIndex requires AVX2+FMA.");
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

        ref var refsHead = ref MemoryMarshal.GetReference(refs);
        ref var queryHead = ref MemoryMarshal.GetReference(query);

        var qLo = Vector256.LoadUnsafe(ref queryHead);
        var qHi = Vector256.LoadUnsafe(ref Unsafe.Add(ref queryHead, 8)) & TailMask;

        for (var i = 0; i < count; i++)
        {
            ref var refHead = ref Unsafe.Add(ref refsHead, i * dims);
            var rLo = Vector256.LoadUnsafe(ref refHead);
            var rHi = Vector256.LoadUnsafe(ref Unsafe.Add(ref refHead, 8)) & TailMask;

            var dLo = qLo - rLo;
            var dHi = qHi - rHi;
            var acc = Fma.MultiplyAdd(dLo, dLo, dHi * dHi);
            var dist = Vector256.Sum(acc);

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
}
