namespace FuzzyFrenzy.Domain
{
    public readonly struct FallMove
    {
        public readonly GridPos From;
        public readonly GridPos To;

        public FallMove(GridPos from, GridPos to)
        {
            From = from;
            To = to;
        }
    }
}
