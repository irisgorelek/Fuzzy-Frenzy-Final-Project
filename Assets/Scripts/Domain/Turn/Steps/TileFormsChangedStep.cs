using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    // This step is required because the model changes before Unity begins animating.
    public sealed class TileFormsChangedStep : TurnStep
    {
        public TileFormsChangedStep(IReadOnlyList<TileFormChange> changes)
        {
            Changes = changes;
        }

        public IReadOnlyList<TileFormChange> Changes { get; }
    }
}
