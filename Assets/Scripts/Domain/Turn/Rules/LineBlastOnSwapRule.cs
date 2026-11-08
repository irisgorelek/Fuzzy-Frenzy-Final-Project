namespace FuzzyFrenzy.Domain
{
    /// <summary>Swapping a tile with <see cref="TileTraits.LineBlastOnSwap"/> sets off its blast where it ends up.</summary>
    public sealed class LineBlastOnSwapRule : ActionRule<SwapAction>
    {
        protected override void React(TurnRun run, SwapAction swap)
        {
            // After the swap, the tile the player dragged is at To and the other tile is at From.
            GridPos center;
            if (run.Board.Has(swap.To, TileTraits.LineBlastOnSwap)) center = swap.To;
            else if (run.Board.Has(swap.From, TileTraits.LineBlastOnSwap)) center = swap.From;
            else return;

            bool swipedVertically = swap.From.X == swap.To.X;
            run.Enqueue(new LineBlastAction(center, swipedVertically));
        }
    }
}
