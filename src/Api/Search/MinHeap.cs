namespace Rinha.Fraud.Search;

internal sealed class MinHeap
{
    private readonly float[] _dist;
    private readonly int[] _id;
    private int _count;

    public MinHeap(int capacity)
    {
        _dist = new float[capacity];
        _id = new int[capacity];
    }

    public int Count => _count;

    public void Clear() => _count = 0;

    public void Push(float dist, int id)
    {
        var i = _count++;
        _dist[i] = dist;
        _id[i] = id;
        while (i > 0)
        {
            var parent = (i - 1) >> 1;
            if (_dist[parent] <= _dist[i]) break;
            (_dist[parent], _dist[i]) = (_dist[i], _dist[parent]);
            (_id[parent], _id[i]) = (_id[i], _id[parent]);
            i = parent;
        }
    }

    public (float Dist, int Id) PeekMin() => (_dist[0], _id[0]);

    public (float Dist, int Id) PopMin()
    {
        var result = (_dist[0], _id[0]);
        _count--;
        if (_count > 0)
        {
            _dist[0] = _dist[_count];
            _id[0] = _id[_count];
            SiftDown(0);
        }
        return result;
    }

    private void SiftDown(int i)
    {
        while (true)
        {
            var left = (i << 1) + 1;
            if (left >= _count) return;
            var right = left + 1;
            var smallest = (right < _count && _dist[right] < _dist[left]) ? right : left;
            if (_dist[smallest] >= _dist[i]) return;
            (_dist[smallest], _dist[i]) = (_dist[i], _dist[smallest]);
            (_id[smallest], _id[i]) = (_id[i], _id[smallest]);
            i = smallest;
        }
    }
}
