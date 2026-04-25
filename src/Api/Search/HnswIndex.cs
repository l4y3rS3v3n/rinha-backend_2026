using Rinha.Fraud.Dataset;

namespace Rinha.Fraud.Search;

internal sealed class HnswIndex : IVectorIndex
{
    private readonly ReferenceDataset _dataset;
    private readonly HnswGraph _graph;
    private readonly int _efSearch;

    [ThreadStatic] private static Scratch? _scratch;

    public HnswIndex(ReferenceDataset dataset, HnswGraph graph, HnswOptions options)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(options);
        HnswDistance.RequireSupport();

        _dataset = dataset;
        _graph = graph;
        _efSearch = options.EfSearch;
    }

    public void Search(ReadOnlySpan<float> query, Span<int> topK)
    {
        if (topK.Length == 0)
            throw new ArgumentException("topK must be non-empty", nameof(topK));

        var k = topK.Length;
        var ef = Math.Max(_efSearch, k);
        var scratch = GetOrCreateScratch(_dataset.Count, ef);

        var ep = _graph.EntryPoint;
        var epDist = HnswDistance.SquaredDistance(query, _dataset.VectorAt(ep));

        for (var l = _graph.MaxLevel; l > 0; l--)
        {
            var changed = true;
            while (changed)
            {
                changed = false;
                var list = _graph.NeighboursOf(ep, l);
                for (var i = 0; i < list.Length; i++)
                {
                    var cand = list[i];
                    var d = HnswDistance.SquaredDistance(query, _dataset.VectorAt(cand));
                    if (d < epDist)
                    {
                        ep = cand;
                        epDist = d;
                        changed = true;
                    }
                }
            }
        }

        scratch.BeginQuery();
        scratch.Candidates.Clear();
        scratch.Results.Clear();
        scratch.MarkVisited(ep);
        scratch.Candidates.Push(epDist, ep);
        scratch.Results.Push(epDist, ep);

        while (scratch.Candidates.Count > 0)
        {
            var c = scratch.Candidates.PopMin();
            if (scratch.Results.Count >= ef && c.Dist > scratch.Results.PeekMaxDist())
                break;

            var list = _graph.NeighboursOf(c.Id, 0);
            for (var i = 0; i < list.Length; i++)
            {
                var n = list[i];
                if (scratch.IsVisited(n)) continue;
                scratch.MarkVisited(n);

                var d = HnswDistance.SquaredDistance(query, _dataset.VectorAt(n));

                if (scratch.Results.Count < ef)
                {
                    scratch.Candidates.Push(d, n);
                    scratch.Results.Push(d, n);
                }
                else if (d < scratch.Results.PeekMaxDist())
                {
                    scratch.Candidates.Push(d, n);
                    scratch.Results.PopMax();
                    scratch.Results.Push(d, n);
                }
            }
        }

        var resultsCount = scratch.Results.Count;
        Span<float> dBuf = stackalloc float[resultsCount];
        Span<int> idBuf = stackalloc int[resultsCount];
        scratch.Results.CopyTo(dBuf, idBuf);

        for (var i = 1; i < resultsCount; i++)
        {
            var keyD = dBuf[i];
            var keyId = idBuf[i];
            var j = i - 1;
            while (j >= 0 && dBuf[j] > keyD)
            {
                dBuf[j + 1] = dBuf[j];
                idBuf[j + 1] = idBuf[j];
                j--;
            }
            dBuf[j + 1] = keyD;
            idBuf[j + 1] = keyId;
        }

        var take = Math.Min(k, resultsCount);
        for (var i = 0; i < take; i++)
            topK[i] = idBuf[i];
        for (var i = take; i < k; i++)
            topK[i] = -1;
    }

    private static Scratch GetOrCreateScratch(int nodeCount, int ef)
    {
        var s = _scratch;
        if (s is null || s.NodeCount < nodeCount || s.Ef < ef)
        {
            s = new Scratch(nodeCount, ef);
            _scratch = s;
        }
        return s;
    }

    private sealed class Scratch
    {
        public readonly MinHeap Candidates;
        public readonly MaxHeap Results;
        public readonly int NodeCount;
        public readonly int Ef;
        private readonly int[] _visitedGen;
        private int _currentGen;

        public Scratch(int nodeCount, int ef)
        {
            NodeCount = nodeCount;
            Ef = ef;
            Candidates = new MinHeap(Math.Max(1024, ef * 4));
            Results = new MaxHeap(ef);
            _visitedGen = new int[nodeCount];
        }

        public void BeginQuery() => _currentGen++;

        public bool IsVisited(int id) => _visitedGen[id] == _currentGen;

        public void MarkVisited(int id) => _visitedGen[id] = _currentGen;
    }
}
