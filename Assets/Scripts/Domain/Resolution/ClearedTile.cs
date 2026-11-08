namespace FuzzyFrenzy.Domain
{
    /// <summary>A tile removed from a specific board position.</summary>
    public readonly struct ClearedTile
    {
        public readonly GridPos Cell;
        public readonly int Type;

        public ClearedTile(GridPos cell, int type)
        {
            Cell = cell;
            Type = type;
        }
    }
}
