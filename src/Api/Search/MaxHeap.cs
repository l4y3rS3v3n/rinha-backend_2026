namespace Rinha.Fraud.Search;

internal sealed class MaxHeap
{
    private readonly float[] _dist;
    private readonly int[] _id;
    private int _count;

    public MaxHeap(int capacity)
    {
        _dist = new float[capacity];
        _id = new int[capacity];
    }

    public int Count => _count;

    public int Capacity => _dist.Length;

    public void Clear() => _count = 0;

    public float PeekMaxDist() => _dist[0];

    public void Push(float dist, int id)
    {
        var i = _count++;
        _dist[i] = dist;
        _id[i] = id;
        while (i > 0)
        {
            var parent = (i - 1) >> 1;
            if (_dist[parent] >= _dist[i]) break;
            (_dist[parent], _dist[i]) = (_dist[i], _dist[parent]);
            (_id[parent], _id[i]) = (_id[i], _id[parent]);
            i = parent;
        }
    }

    public void PopMax()
    {
        _count--;
        if (_count > 0)
        {
            _dist[0] = _dist[_count];
            _id[0] = _id[_count];
            SiftDown(0);
        }
    }

    public void CopyTo(Span<float> distOut, Span<int> idOut)
    {
        _dist.AsSpan(0, _count).CopyTo(distOut);
        _id.AsSpan(0, _count).CopyTo(idOut);
    }

    private void SiftDown(int i)
    {
        while (true)
        {
            var left = (i << 1) + 1;
            if (left >= _count) return;
            var right = left + 1;
            var largest = (right < _count && _dist[right] > _dist[left]) ? right : left;
            if (_dist[largest] <= _dist[i]) return;
            (_dist[largest], _dist[i]) = (_dist[i], _dist[largest]);
            (_id[largest], _id[i]) = (_id[i], _id[largest]);
            i = largest;
        }
    }
}
