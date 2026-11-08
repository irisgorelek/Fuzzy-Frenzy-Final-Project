using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Bones broke. They're counted as destroyed but give no points.</summary>
    public sealed class TilesBrokenStep : TurnStep
    {
        public TilesBrokenStep(IReadOnlyList<GridPos> cells, IReadOnlyList<string> ids) { Cells = cells; Ids = ids; }
        public IReadOnlyList<GridPos> Cells { get; }
        /// <summary>The tile ID of each broken cell, in the same order as <see cref="Cells"/>.</summary>
        public IReadOnlyList<string> Ids { get; }
    }
}
