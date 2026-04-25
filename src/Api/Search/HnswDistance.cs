using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Rinha.Fraud.Vectorization;

namespace Rinha.Fraud.Search;

internal static class HnswDistance
{
    private static readonly Vector256<float> TailMask = Vector256.Create(
        -1f, -1f, -1f, -1f, -1f, -1f, 0f, 0f);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float SquaredDistance(ReadOnlySpan<float> a, ReadOnlySpan<float> b)
    {
        ref var aHead = ref MemoryMarshal.GetReference(a);
        ref var bHead = ref MemoryMarshal.GetReference(b);

        var aLo = Vector256.LoadUnsafe(ref aHead);
        var aHi = Vector256.LoadUnsafe(ref Unsafe.Add(ref aHead, 8)) & TailMask;
        var bLo = Vector256.LoadUnsafe(ref bHead);
        var bHi = Vector256.LoadUnsafe(ref Unsafe.Add(ref bHead, 8)) & TailMask;

        var dLo = aLo - bLo;
        var dHi = aHi - bHi;
        var acc = Fma.MultiplyAdd(dLo, dLo, dHi * dHi);
        return Vector256.Sum(acc);
    }

    public static void RequireSupport()
    {
        if (!Avx2.IsSupported || !Fma.IsSupported)
            throw new PlatformNotSupportedException("HNSW distance requires AVX2+FMA.");
#pragma warning disable CA1508                                
        if (NormalizationConstants.VectorDimensions != 14)
            throw new InvalidOperationException(
                "HNSW distance is hard-coded for 14-dim vectors.");
#pragma warning restore CA1508
    }
}
