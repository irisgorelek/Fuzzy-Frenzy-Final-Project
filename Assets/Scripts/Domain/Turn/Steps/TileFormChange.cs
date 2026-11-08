namespace FuzzyFrenzy.Domain
{
    public readonly struct TileFormChange
    {
        public readonly GridPos Cell;
        public readonly int OldType;
        public readonly int NewType;

        public TileFormChange(GridPos cell, int oldType, int newType)
        {
            Cell = cell;
            OldType = oldType;
            NewType = newType;
        }
    }
}
