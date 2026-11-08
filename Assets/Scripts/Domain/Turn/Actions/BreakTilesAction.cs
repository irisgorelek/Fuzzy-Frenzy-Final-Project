using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Breaks bones (tiles with <see cref="TileTraits.BreaksNextToClear"/>). They count as destroyed for goals but give no points.</summary>
    public sealed class BreakTilesAction : IBoardAction
    {
        private readonly IReadOnlyList<GridPos> _cells;

        public BreakTilesAction(IReadOnlyList<GridPos> cells) { _cells = cells; }

        public void Apply(TurnRun run)
        {
            var broken = new List<GridPos>(_cells.Count);
            var ids = new List<string>(_cells.Count);

            for (int i = 0; i < _cells.Count; i++)
            {
                if (!Break(run, _cells[i], out string id)) continue;
                broken.Add(_cells[i]);
                ids.Add(id);
            }

            if (broken.Count > 0)
                run.Record(new TilesBrokenStep(broken, ids));
        }

        /// <summary>Breaks one bone and counts it. False if the cell holds no bone.</summary>
        internal static bool Break(TurnRun run, GridPos cell, out string id)
        {
            var def = run.Board.DefinitionAt(cell);
            id = def?.Id;
            if (def == null || !def.Has(TileTraits.BreaksNextToClear)) return false;

            run.Board.Clear(cell);
            run.Session.AddDestroyed(id, 1);
            return true;
        }
    }
}
