using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// The black sheep's blast, as waves spreading from the sheep. Bones next to each wave break as it
    /// passes; then the whole line is cleared.
    /// </summary>
    public sealed class LineBlastAction : IBoardAction
    {
        public LineBlastAction(GridPos center, bool swipedVertically)
        {
            Center = center;
            SwipedVertically = swipedVertically;
        }

        public GridPos Center { get; }
        public bool SwipedVertically { get; }

        public void Apply(TurnRun run)
        {
            var board = run.Board;
            var waveCells = new List<List<GridPos>>();
            var line = new List<GridPos>();
            BoardOperations.BuildBlastWaves(board, Center, SwipedVertically, waveCells, line);

            var waves = new List<BlastWave>(waveCells.Count);
            for (int w = 0; w < waveCells.Count; w++)
            {
                var cells = waveCells[w];
                var pops = new List<GridPos>(cells.Count);
                for (int i = 0; i < cells.Count; i++)
                    if (!board.IsEmpty(cells[i]) && !board.Has(cells[i], TileTraits.ResistsBlasts))
                        pops.Add(cells[i]);

                var bones = new List<GridPos>();
                var boneIds = new List<string>();
                BoardOperations.FindBreakableNeighbours(board, cells, bones);
                for (int i = 0; i < bones.Count; i++)
                {
                    BreakTilesAction.Break(run, bones[i], out string id);
                    boneIds.Add(id);
                }

                waves.Add(new BlastWave(cells, pops, bones, boneIds));
            }

            run.Record(new LineBlastStep(Center, SwipedVertically, waves));
            run.Enqueue(new ClearAction(line, ClearCause.Explosion, showPop: false));
        }
    }
}
