namespace Rinha.Fraud.Search;

internal sealed class HnswGraph
{
    private readonly int[] _levels;
    private readonly int[][][] _neighbours;
    public int EntryPoint { get; }
    public int MaxLevel { get; }
    public int Count => _levels.Length;

    public HnswGraph(int[] levels, int[][][] neighbours, int entryPoint, int maxLevel)
    {
        ArgumentNullException.ThrowIfNull(levels);
        ArgumentNullException.ThrowIfNull(neighbours);
        if (levels.Length != neighbours.Length)
            throw new ArgumentException("levels and neighbours must have equal length");

        _levels = levels;
        _neighbours = neighbours;
        EntryPoint = entryPoint;
        MaxLevel = maxLevel;
    }

    public int LevelOf(int nodeId) => _levels[nodeId];

    public ReadOnlySpan<int> NeighboursOf(int nodeId, int level) =>
        _neighbours[nodeId][level];
}
