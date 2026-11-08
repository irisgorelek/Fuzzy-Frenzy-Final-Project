namespace FuzzyFrenzy.Domain
{
    /// <summary>The board had no legal move, so it was shuffled.</summary>
    public sealed class ShuffleStep : TurnStep
    {
        public ShuffleStep(BoardSnapshot after) { After = after; }
        public BoardSnapshot After { get; }
    }
}
