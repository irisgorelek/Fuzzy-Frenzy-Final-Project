using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Everything that happened in one turn, in order. The view replays it.</summary>
    public sealed class TurnResult
    {
        private static readonly IReadOnlyList<TurnStep> NoSteps = new TurnStep[0];

        public TurnResult(TurnOutcome outcome, RejectReason reason, IReadOnlyList<TurnStep> steps, bool goalsComplete)
        {
            Outcome = outcome;
            RejectReason = reason;
            Steps = steps ?? NoSteps;
            GoalsComplete = goalsComplete;
        }

        public TurnOutcome Outcome { get; }
        public RejectReason RejectReason { get; }
        public IReadOnlyList<TurnStep> Steps { get; }

        /// <summary>All level goals were met. The turn stopped at that point.</summary>
        public bool GoalsComplete { get; }

        internal static TurnResult Rejected(RejectReason reason) => new TurnResult(TurnOutcome.Rejected, reason, null, false);
    }
}
