namespace FuzzyFrenzy.Domain
{
    /// <summary>Two tiles swapped places.</summary>
    public sealed class SwapStep : TurnStep
    {
        public SwapStep(GridPos from, GridPos to) { From = from; To = to; }
        public GridPos From { get; }
        public GridPos To { get; }
    }
}
