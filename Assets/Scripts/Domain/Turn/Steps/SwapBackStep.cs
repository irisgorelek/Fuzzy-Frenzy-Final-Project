namespace FuzzyFrenzy.Domain
{
    /// <summary>A swap made no match, so the tiles went back.</summary>
    public sealed class SwapBackStep : TurnStep
    {
        public SwapBackStep(GridPos from, GridPos to) { From = from; To = to; }
        public GridPos From { get; }
        public GridPos To { get; }
    }
}
