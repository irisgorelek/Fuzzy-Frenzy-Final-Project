using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// A matched tile with PermanentlyWakesAdjacent converts
    /// adjacent cycling animals into their permanent forms.
    /// </summary>
    public sealed class PermanentWakeRule : ActionRule<ClearAction>
    {
        private readonly List<GridPos> _targets = new List<GridPos>();

        private readonly HashSet<GridPos> _claimed = new HashSet<GridPos>();

        protected override void React(TurnRun run, ClearAction clear)
        {
            _targets.Clear();
            _claimed.Clear();

            // A booster or explosion removing a goose
            // does not activate its match ability.
            if (clear.Cause != ClearCause.Match || clear.Report == null)
            {
                return;
            }

            IReadOnlyList<ClearedTile> removed = clear.Report.ClearedTiles;

            for (int i = 0; i < removed.Count; i++)
            {
                ClearedTile cleared = removed[i];

                TileDefinition source = run.Board.Catalog.Get(cleared.Type);

                if (source == null || !source.Has(TileTraits.PermanentlyWakesAdjacent))
                {
                    continue;
                }

                FindAdjacentTargets(run.Board, cleared.Cell);
            }

            if (_targets.Count > 0)
            {
                run.Enqueue(new PermanentWakeTilesAction(_targets.ToArray()));
            }
        }

        private void FindAdjacentTargets(BoardState board, GridPos gooseCell)
        {
            for (int i = 0; i < GridPos.Orthogonal.Length; i++)
            {
                GridPos target = gooseCell + GridPos.Orthogonal[i];

                TileDefinition definition = board.DefinitionAt(target);

                if (definition == null || !definition.Has(TileTraits.CanBePermanentlyAwakened))
                {
                    continue;
                }

                // Two matched geese may touch the same pig.
                // Only change that pig once.
                if (_claimed.Add(target))
                {
                    _targets.Add(target);
                }
            }
        }
    }
}