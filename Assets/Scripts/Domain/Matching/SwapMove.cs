namespace FuzzyFrenzy.Domain
{
    public readonly struct SwapMove
    {
        public readonly GridPos From;
        public readonly GridPos To;

        public SwapMove(GridPos from, GridPos to)
        {
            From = from;
            To = to;
        }
    }
}
