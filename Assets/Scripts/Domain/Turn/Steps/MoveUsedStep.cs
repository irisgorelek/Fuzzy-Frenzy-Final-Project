namespace FuzzyFrenzy.Domain
{
    /// <summary>The swap was accepted and used a move.</summary>
    public sealed class MoveUsedStep : TurnStep
    {
        public MoveUsedStep(bool byMatch) { ByMatch = byMatch; }
        /// <summary>True when the swap was accepted because it made a match (rather than setting off a special).</summary>
        public bool ByMatch { get; }
    }
}
