using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Tiles were removed and scored.</summary>
    public sealed class ClearStep : TurnStep
    {
        public ClearStep(ClearCause cause, ClearReport report)
        {
            Cause = cause;
            Cleared = report.Cleared;
            Points = report.Points;
            Matched = report.Matched;
            Destroyed = report.Destroyed;
        }

        public ClearCause Cause { get; }
        public IReadOnlyList<GridPos> Cleared { get; }
        public int Points { get; }
        public int Matched { get; }
        public IReadOnlyList<DestroyedCount> Destroyed { get; }
    }
}
