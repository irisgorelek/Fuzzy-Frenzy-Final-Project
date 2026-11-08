namespace FuzzyFrenzy.Domain
{
    /// <summary>Shuffles a board that has no legal move until it has one.</summary>
    public sealed class ShuffleAction : IBoardAction
    {
        public const int MaxAttempts = 100;

        public void Apply(TurnRun run)
        {
            BoardOperations.ShuffleUntilPlayable(run.Board, run.Random, MaxAttempts);
            run.Record(new ShuffleStep(run.Board.Snapshot()));
        }
    }
}
