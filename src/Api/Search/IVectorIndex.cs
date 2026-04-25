namespace Rinha.Fraud.Search;

internal interface IVectorIndex
{
    void Search(ReadOnlySpan<float> query, Span<int> topK);
}
