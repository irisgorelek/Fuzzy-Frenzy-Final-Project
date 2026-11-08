namespace FuzzyFrenzy.Domain
{
    /// <summary>Marks the moment the board has stopped cascading. Rules that act on a settled board react to it.</summary>
    public sealed class BoardSettledAction : IBoardAction
    {
        public void Apply(TurnRun run) { }
    }
}
