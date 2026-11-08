using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Tiles with <see cref="TileTraits.BreaksNextToClear"/> (bones) break when a tile next to them is cleared.</summary>
    public sealed class BreakNextToClearRule : ActionRule<ClearAction>
    {
        private readonly List<GridPos> _buffer = new List<GridPos>();

        protected override void React(TurnRun run, ClearAction clear)
        {
            BoardOperations.FindBreakableNeighbours(run.Board, clear.DamageSources, _buffer);
            if (_buffer.Count > 0)
                run.Enqueue(new BreakTilesAction(_buffer.ToArray()));
        }
    }
}
