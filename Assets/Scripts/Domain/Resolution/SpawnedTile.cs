namespace FuzzyFrenzy.Domain
{
    public readonly struct SpawnedTile
    {
        /// <summary>Where the new tile lands.</summary>
        public readonly GridPos Cell;
        public readonly int Type;
        /// <summary>-1 when it enters from above the board, otherwise the row of the blocker it enters below.</summary>
        public readonly int SpawnFromY;

        public SpawnedTile(GridPos cell, int type, int spawnFromY)
        {
            Cell = cell;
            Type = type;
            SpawnFromY = spawnFromY;
        }
    }
}
