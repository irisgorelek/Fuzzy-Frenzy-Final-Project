using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>The player's swap.</summary>
    public sealed class SwapAction : IBoardAction
    {
        public SwapAction(GridPos from, GridPos to) { From = from; To = to; }
        public GridPos From { get; }
        public GridPos To { get; }

        public void Apply(TurnRun run)
        {
            run.Board.Swap(From, To);
            run.Record(new SwapStep(From, To));
        }
    }

    /// <summary>Removes tiles and scores them.</summary>
    public sealed class ClearAction : IBoardAction
    {
        private readonly IReadOnlyList<GridPos> _cells;
        private readonly bool _showPop;
        private ClearReport _report;

        public ClearAction(IReadOnlyList<GridPos> cells, ClearCause cause, bool showPop)
        {
            _cells = cells;
            Cause = cause;
            _showPop = showPop;
        }

        public ClearCause Cause { get; }

        /// <summary>Cells whose neighbours take damage from this clear (bones next to them break).</summary>
        public IReadOnlyList<GridPos> DamageSources => Cause == ClearCause.Booster ? (IReadOnlyList<GridPos>)_report?.Cleared : _cells;

        public void Apply(TurnRun run)
        {
            if (_showPop)
                run.Record(new MatchPopStep(_cells));

            _report = BoardOperations.Clear(run.Board, _cells, Cause);

            run.Session.AddPoints(_report.Points);
            run.Session.AddMatched(_report.Matched);
            for (int i = 0; i < _report.Destroyed.Count; i++)
                run.Session.AddDestroyed(_report.Destroyed[i].Id, _report.Destroyed[i].Count);

            run.Record(new ClearStep(Cause, _report));
        }
    }

    /// <summary>Breaks bones. They count as destroyed for goals but give no points.</summary>
    public sealed class BreakBonesAction : IBoardAction
    {
        private readonly IReadOnlyList<GridPos> _cells;

        public BreakBonesAction(IReadOnlyList<GridPos> cells) { _cells = cells; }

        public void Apply(TurnRun run)
        {
            string boneId = run.Board.Catalog.Get(run.Board.Specials.Bone)?.Id;
            var broken = new List<GridPos>(_cells.Count);

            for (int i = 0; i < _cells.Count; i++)
            {
                if (!run.Board.IsBone(_cells[i])) continue;
                run.Board.Clear(_cells[i]);
                run.Session.AddDestroyed(boneId, 1);
                broken.Add(_cells[i]);
            }

            if (broken.Count > 0)
                run.Record(new BonesBrokenStep(broken, boneId));
        }
    }

    /// <summary>Tiles fall into the gaps and new tiles fill the board.</summary>
    public sealed class GravityRefillAction : IBoardAction
    {
        private readonly float _duration;

        public GravityRefillAction(float duration) { _duration = duration; }

        public void Apply(TurnRun run)
        {
            var falls = new List<FallMove>();
            var spawns = new List<SpawnedTile>();
            GravitySolver.Apply(run.Board, falls);
            RefillPlanner.Fill(run.Board, run.Random, spawns);
            run.Record(new GravityStep(falls, spawns, run.Board.Snapshot(), _duration));
        }
    }

    /// <summary>
    /// The black sheep's blast, as waves spreading from the sheep. Bones next to each wave break as it
    /// passes; then the whole line is cleared.
    /// </summary>
    public sealed class BlackSheepBlastAction : IBoardAction
    {
        public BlackSheepBlastAction(GridPos center, bool swipedVertically)
        {
            Center = center;
            SwipedVertically = swipedVertically;
        }

        public GridPos Center { get; }
        public bool SwipedVertically { get; }

        public void Apply(TurnRun run)
        {
            var board = run.Board;
            string boneId = board.Catalog.Get(board.Specials.Bone)?.Id;
            var waveCells = new List<List<GridPos>>();
            var line = new List<GridPos>();
            BoardOperations.BuildBlastWaves(board, Center, SwipedVertically, waveCells, line);

            var waves = new List<BlastWave>(waveCells.Count);
            for (int w = 0; w < waveCells.Count; w++)
            {
                var cells = waveCells[w];
                var pops = new List<GridPos>(cells.Count);
                for (int i = 0; i < cells.Count; i++)
                    if (!board.IsEmpty(cells[i]) && !board.IsBone(cells[i]))
                        pops.Add(cells[i]);

                var bones = new List<GridPos>();
                BoardOperations.FindAdjacentBones(board, cells, bones);
                for (int i = 0; i < bones.Count; i++)
                {
                    board.Clear(bones[i]);
                    run.Session.AddDestroyed(boneId, 1);
                }

                waves.Add(new BlastWave(cells, pops, bones));
            }

            run.Record(new BlackSheepBlastStep(Center, SwipedVertically, waves, boneId));
            run.Enqueue(new ClearAction(line, ClearCause.Explosion, showPop: false));
        }
    }

    /// <summary>Marks the moment the board has stopped cascading. Rules that act on a settled board react to it.</summary>
    public sealed class BoardSettledAction : IBoardAction
    {
        public void Apply(TurnRun run) { }
    }

    /// <summary>Wolves eat the sheep next to them; each sheep becomes bones.</summary>
    public sealed class WolfFeastAction : IBoardAction
    {
        private readonly IReadOnlyList<WolfMeal> _meals;

        public WolfFeastAction(IReadOnlyList<WolfMeal> meals) { _meals = meals; }

        public void Apply(TurnRun run)
        {
            var board = run.Board;
            string sheepId = board.Catalog.Get(board.Specials.Sheep)?.Id;
            int points = BoardOperations.ApplyWolfMeals(board, _meals, out int eaten);

            run.Session.AddPoints(points);
            run.Session.AddMatched(eaten);
            run.Session.AddDestroyed(sheepId, eaten);

            run.Record(new WolfFeastStep(_meals, points, eaten, sheepId, board.Specials.Bone));
        }
    }

    /// <summary>Shuffles a board that has no legal move until it has one.</summary>
    public sealed class ShuffleAction : IBoardAction
    {
        public const int MaxAttempts = 100;

        public void Apply(TurnRun run)
        {
            BoardOperations.ShuffleUntilPlayable(run.Board, run.Random, MaxAttempts);
            run.Record(new ShuffleStep(run.Board.Snapshot()));
        }
    }
}
