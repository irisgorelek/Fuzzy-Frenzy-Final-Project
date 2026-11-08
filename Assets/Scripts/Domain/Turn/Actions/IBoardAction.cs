namespace FuzzyFrenzy.Domain
{
    /// <summary>Something that happens to the board. Applying it changes the board and records a step.</summary>
    public interface IBoardAction
    {
        void Apply(TurnRun run);
    }
}
