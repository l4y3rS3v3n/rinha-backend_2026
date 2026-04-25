using Rinha.Fraud.Dataset;

namespace Rinha.Fraud.Search;

internal static class HnswBuilder
{
    public static HnswGraph Build(ReferenceDataset dataset, HnswOptions options)
    {
        ArgumentNullException.ThrowIfNull(dataset);
        ArgumentNullException.ThrowIfNull(options);
        HnswDistance.RequireSupport();

        var n = dataset.Count;
        if (n == 0) throw new ArgumentException("empty dataset", nameof(dataset));

        var mL = 1.0 / Math.Log(options.M);
#pragma warning disable CA5394 // deterministic seed, not a security use
        var rng = new Random(unchecked((int)options.Seed));
#pragma warning restore CA5394

        var levels = new int[n];
        for (var i = 0; i < n; i++)
            levels[i] = SampleLevel(rng, mL);

        var neighbours = new List<int>[n][];
        for (var i = 0; i < n; i++)
        {
            neighbours[i] = new List<int>[levels[i] + 1];
            for (var l = 0; l <= levels[i]; l++)
                neighbours[i][l] = new List<int>(l == 0 ? options.MMax0 : options.M);
        }

        var entryPoint = 0;
        var currentMaxLevel = levels[0];

        var state = new BuilderState(n, options.EfConstruction);

        for (var i = 1; i < n; i++)
        {
            Insert(i, dataset, options, levels, neighbours, state,
                ref entryPoint, ref currentMaxLevel);
        }

        var frozen = new int[n][][];
        for (var i = 0; i < n; i++)
        {
            var l = levels[i];
            var perNode = new int[l + 1][];
            for (var layer = 0; layer <= l; layer++)
                perNode[layer] = neighbours[i][layer].ToArray();
            frozen[i] = perNode;
        }

        return new HnswGraph(levels, frozen, entryPoint, currentMaxLevel);
    }

    private static int SampleLevel(Random rng, double mL)
    {
        double u;
        do { u = rng.NextDouble(); } while (u <= 0d);
        return (int)(-Math.Log(u) * mL);
    }

    private static void Insert(
        int nodeId,
        ReferenceDataset ds,
        HnswOptions opt,
        int[] levels,
        List<int>[][] neighbours,
        BuilderState state,
        ref int entryPoint,
        ref int currentMaxLevel)
    {
        var query = ds.VectorAt(nodeId);
        var nodeLevel = levels[nodeId];

        var ep = entryPoint;
        var epDist = HnswDistance.SquaredDistance(query, ds.VectorAt(ep));

        for (var l = currentMaxLevel; l > nodeLevel; l--)
            (ep, epDist) = Greedy1NN(ds, query, ep, epDist, neighbours, l);

        for (var l = Math.Min(currentMaxLevel, nodeLevel); l >= 0; l--)
        {
            SearchLayer(ds, query, ep, epDist, opt.EfConstruction, l, neighbours, state);

            var layerCap = l == 0 ? opt.MMax0 : opt.M;
            var selected = state.DrainSortedAscending();
            var toTake = Math.Min(opt.M, selected.Length);

            var myList = neighbours[nodeId][l];
            myList.Clear();
            for (var i = 0; i < toTake; i++)
                myList.Add(selected[i].Id);

            for (var i = 0; i < toTake; i++)
            {
                var otherId = selected[i].Id;
                var otherList = neighbours[otherId][l];
                otherList.Add(nodeId);
                if (otherList.Count > layerCap)
                    PruneNeighbours(ds, otherId, otherList, layerCap);
            }

            if (selected.Length > 0)
            {
                ep = selected[0].Id;
                epDist = selected[0].Dist;
            }
        }

        if (nodeLevel > currentMaxLevel)
        {
            entryPoint = nodeId;
            currentMaxLevel = nodeLevel;
        }
    }

    private static (int Id, float Dist) Greedy1NN(
        ReferenceDataset ds,
        ReadOnlySpan<float> query,
        int startId,
        float startDist,
        List<int>[][] neighbours,
        int layer)
    {
        var bestId = startId;
        var bestDist = startDist;
        var changed = true;
        while (changed)
        {
            changed = false;
            var list = neighbours[bestId][layer];
            for (var i = 0; i < list.Count; i++)
            {
                var cand = list[i];
                var d = HnswDistance.SquaredDistance(query, ds.VectorAt(cand));
                if (d < bestDist)
                {
                    bestDist = d;
                    bestId = cand;
                    changed = true;
                }
            }
        }
        return (bestId, bestDist);
    }

    private static void SearchLayer(
        ReferenceDataset ds,
        ReadOnlySpan<float> query,
        int entry,
        float entryDist,
        int ef,
        int layer,
        List<int>[][] neighbours,
        BuilderState state)
    {
        state.BeginQuery();
        state.Candidates.Clear();
        state.Results.Clear();

        state.MarkVisited(entry);
        state.Candidates.Push(entryDist, entry);
        state.Results.Push(entryDist, entry);

        while (state.Candidates.Count > 0)
        {
            var c = state.Candidates.PopMin();
            if (state.Results.Count >= ef && c.Dist > state.Results.PeekMaxDist())
                break;

            var list = neighbours[c.Id][layer];
            for (var i = 0; i < list.Count; i++)
            {
                var n = list[i];
                if (state.IsVisited(n)) continue;
                state.MarkVisited(n);

                var d = HnswDistance.SquaredDistance(query, ds.VectorAt(n));

                if (state.Results.Count < ef)
                {
                    state.Candidates.Push(d, n);
                    state.Results.Push(d, n);
                }
                else if (d < state.Results.PeekMaxDist())
                {
                    state.Candidates.Push(d, n);
                    state.Results.PopMax();
                    state.Results.Push(d, n);
                }
            }
        }
    }

    private static void PruneNeighbours(
        ReferenceDataset ds,
        int ownerId,
        List<int> list,
        int cap)
    {
        var owner = ds.VectorAt(ownerId);
        Span<float> dists = stackalloc float[list.Count];
        for (var i = 0; i < list.Count; i++)
            dists[i] = HnswDistance.SquaredDistance(owner, ds.VectorAt(list[i]));

        for (var i = 1; i < list.Count; i++)
        {
            var keyD = dists[i];
            var keyId = list[i];
            var j = i - 1;
            while (j >= 0 && dists[j] > keyD)
            {
                dists[j + 1] = dists[j];
                list[j + 1] = list[j];
                j--;
            }
            dists[j + 1] = keyD;
            list[j + 1] = keyId;
        }

        if (list.Count > cap)
            list.RemoveRange(cap, list.Count - cap);
    }

    private sealed class BuilderState
    {
        public readonly MinHeap Candidates;
        public readonly MaxHeap Results;
        private readonly int[] _visitedGen;
        private int _currentGen;
        private (float Dist, int Id)[] _drainBuf;

        public BuilderState(int nodeCount, int efConstruction)
        {
            Candidates = new MinHeap(Math.Max(1024, efConstruction * 4));
            Results = new MaxHeap(efConstruction);
            _visitedGen = new int[nodeCount];
            _drainBuf = new (float, int)[efConstruction];
        }

        public void BeginQuery() => _currentGen++;

        public bool IsVisited(int id) => _visitedGen[id] == _currentGen;

        public void MarkVisited(int id) => _visitedGen[id] = _currentGen;

        public (float Dist, int Id)[] DrainSortedAscending()
        {
            var n = Results.Count;
            if (_drainBuf.Length < n) _drainBuf = new (float, int)[n];
            Span<float> d = stackalloc float[n];
            Span<int> ids = stackalloc int[n];
            Results.CopyTo(d, ids);
            for (var i = 1; i < n; i++)
            {
                var keyD = d[i]; var keyId = ids[i]; var j = i - 1;
                while (j >= 0 && d[j] > keyD)
                { d[j + 1] = d[j]; ids[j + 1] = ids[j]; j--; }
                d[j + 1] = keyD; ids[j + 1] = keyId;
            }
            for (var i = 0; i < n; i++) _drainBuf[i] = (d[i], ids[i]);
            return _drainBuf.AsSpan(0, n).ToArray();
        }
    }
}
