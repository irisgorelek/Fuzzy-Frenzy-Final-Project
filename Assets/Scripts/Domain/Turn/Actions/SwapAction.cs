namespace FuzzyFrenzy.Domain
{
    /// <summary>The player's swap.</summary>
    public sealed class SwapAction : IBoardAction
    {
        public SwapAction(GridPos from, GridPos to) { From = from; To = to; }
        public GridPos From { get; }
        public GridPos To { get; }

        public void Apply(TurnRun run)
        {
            run.Board.Swap(From, To);
            run.Record(new SwapStep(From, To));
        }
    }
}
