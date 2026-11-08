using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// Converts cycling animals into their configured permanent form.
    /// </summary>
    public sealed class PermanentWakeTilesAction : IBoardAction
    {
        private readonly IReadOnlyList<GridPos> _cells;

        public PermanentWakeTilesAction(IReadOnlyList<GridPos> cells)
        {
            _cells = cells;
        }

        public void Apply(TurnRun run)
        {
            var changes = new List<TileFormChange>();

            for (int i = 0; i < _cells.Count; i++)
            {
                GridPos cell = _cells[i];

                TileDefinition current = run.Board.DefinitionAt(cell);

                if (current == null || !current.Has(TileTraits.CanBePermanentlyAwakened))
                {
                    continue;
                }

                int permanentType = run.Board.Catalog.TypeOfId(current.PermanentAwakeFormId);

                if (permanentType < 0 || permanentType == current.Type)
                {
                    continue;
                }

                int oldType = current.Type;

                if (!run.Board.ChangeType(cell, permanentType))
                {
                    continue;
                }

                changes.Add(new TileFormChange(cell, oldType, permanentType));
            }

            if (changes.Count > 0)
            {
                run.Record(new TileFormsChangedStep(changes.ToArray()));
            }
        }
    }
}
