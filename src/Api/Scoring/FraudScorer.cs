using Rinha.Fraud.Contracts;
using Rinha.Fraud.Dataset;
using Rinha.Fraud.Search;
using Rinha.Fraud.Vectorization;

namespace Rinha.Fraud.Scoring;

internal sealed class FraudScorer
{
    private readonly Vectorizer _vectorizer;
    private readonly IVectorIndex _index;
    private readonly ReferenceDataset _dataset;
    private readonly bool _l2Normalize;

    public FraudScorer(Vectorizer vectorizer, IVectorIndex index, ReferenceDataset dataset, bool l2Normalize)
    {
        ArgumentNullException.ThrowIfNull(vectorizer);
        ArgumentNullException.ThrowIfNull(index);
        ArgumentNullException.ThrowIfNull(dataset);

        _vectorizer = vectorizer;
        _index = index;
        _dataset = dataset;
        _l2Normalize = l2Normalize;
    }

    public FraudResponse Score(FraudRequest request)
    {
        const int dims = NormalizationConstants.VectorDimensions;
        const int k = NormalizationConstants.KnnK;

        Span<float> query = stackalloc float[dims];
        _vectorizer.Write(request, query);

        if (_l2Normalize)
            L2Normalizer.NormalizeInPlace(query);

        Span<int> topK = stackalloc int[k];
        _index.Search(query, topK);

        var fraudCount = 0;
        for (var i = 0; i < k; i++)
        {
            if (_dataset.LabelAt(topK[i]) == Label.Fraud)
                fraudCount++;
        }

        var fraudScore = (double)fraudCount / k;
        var approved = fraudScore < NormalizationConstants.FraudThreshold;
        return new FraudResponse(approved, fraudScore);
    }
}
