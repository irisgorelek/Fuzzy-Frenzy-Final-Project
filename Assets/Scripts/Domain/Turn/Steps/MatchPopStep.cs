using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Matched tiles pop, just before they're cleared.</summary>
    public sealed class MatchPopStep : TurnStep
    {
        public MatchPopStep(IReadOnlyList<GridPos> cells) { Cells = cells; }
        public IReadOnlyList<GridPos> Cells { get; }
    }
}
