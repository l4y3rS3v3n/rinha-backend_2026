using Rinha.Fraud.Vectorization;

namespace Rinha.Fraud.Dataset;

internal sealed class ReferenceDataset
{
    private readonly float[] _vectors;
    private readonly Label[] _labels;

    public ReferenceDataset(float[] vectors, Label[] labels)
    {
        ArgumentNullException.ThrowIfNull(vectors);
        ArgumentNullException.ThrowIfNull(labels);

        if (vectors.Length % NormalizationConstants.VectorDimensions != 0)
            throw new ArgumentException(
                $"vectors length ({vectors.Length}) is not a multiple of {NormalizationConstants.VectorDimensions}",
                nameof(vectors));

        var expectedCount = vectors.Length / NormalizationConstants.VectorDimensions;
        if (labels.Length != expectedCount)
            throw new ArgumentException(
                $"labels length ({labels.Length}) does not match vector count ({expectedCount})",
                nameof(labels));

        _vectors = vectors;
        _labels = labels;
        Count = expectedCount;
    }

    public int Count { get; }

    public ReadOnlySpan<float> Vectors => _vectors;

    public ReadOnlySpan<Label> Labels => _labels;

    public ReadOnlySpan<float> VectorAt(int index) =>
        _vectors.AsSpan(index * NormalizationConstants.VectorDimensions, NormalizationConstants.VectorDimensions);

    public Label LabelAt(int index) => _labels[index];
}
