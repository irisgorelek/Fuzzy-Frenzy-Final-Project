using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
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
        public ClearReport Report => _report;


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
}
