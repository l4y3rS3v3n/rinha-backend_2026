using Rinha.Fraud.Vectorization;

namespace Rinha.Fraud.Vectorization;

internal static class L2Normalizer
{
    private const float Epsilon = 1e-12f;

    public static void NormalizeInPlace(Span<float> v)
    {
        var sum = 0f;
        for (var i = 0; i < v.Length; i++)
            sum += v[i] * v[i];

        if (sum <= Epsilon) return;

        var invNorm = 1f / MathF.Sqrt(sum);
        for (var i = 0; i < v.Length; i++)
            v[i] *= invNorm;
    }
}
