using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    public enum TurnOutcome
    {
        /// <summary>The input wasn't allowed. Nothing changed.</summary>
        Rejected,
        /// <summary>The tiles swapped but made no match, so they swapped back. No move used.</summary>
        NoMatch,
        /// <summary>The turn played out.</summary>
        Resolved
    }

    public enum RejectReason
    {
        None,
        /// <summary>One of the cells is empty. Ignored silently.</summary>
        EmptyCell,
        /// <summary>One of the tiles can't be swapped (bones).</summary>
        NotSwappable,
        /// <summary>The cells aren't neighbours or are off the board.</summary>
        NotAdjacent
    }

    /// <summary>The state the core needs from outside to play a turn.</summary>
    public readonly struct TurnContext
    {
        public TurnContext(int movesLeft, int maxMoves, bool timerActive)
        {
            MovesLeft = movesLeft;
            MaxMoves = maxMoves;
            TimerActive = timerActive;
        }

        public int MovesLeft { get; }
        public int MaxMoves { get; }
        public bool TimerActive { get; }
    }

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
