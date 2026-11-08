namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// One thing that happened during a turn, in order. Each step carries everything needed to animate it,
    /// so the view never reads the final board to reconstruct an earlier moment.
    /// </summary>
    public abstract class TurnStep { }
}
