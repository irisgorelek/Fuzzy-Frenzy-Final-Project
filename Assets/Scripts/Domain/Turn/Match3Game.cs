using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>How often a special tile (the black sheep) may be armed for the next refill.</summary>
    public readonly struct SpecialSpawnSettings
    {
        public static SpecialSpawnSettings None => new SpecialSpawnSettings(-1, 0, 0f);

        public SpecialSpawnSettings(int type, int everyMoves, float chance)
        {
            Type = type;
            EveryMoves = everyMoves;
            Chance = chance;
        }

        /// <summary>The tile type to arm, or -1 when the level has none.</summary>
        public int Type { get; }
        public bool Enabled => Type >= 0;
        /// <summary>Roll once every this many moves. 0 or less disables rolling.</summary>
        public int EveryMoves { get; }
        /// <summary>Chance from 0 to 1 for each roll.</summary>
        public float Chance { get; }
    }

    /// <summary>
    /// The turn owner. Every input that changes the board (a swap, a booster, the end of the timer) comes
    /// through here and returns one <see cref="TurnResult"/>. The board is fully resolved before this returns;
    /// the view replays the result afterwards.
    /// </summary>
    public sealed class Match3Game
    {
        private const float CascadeGravitySeconds = 0.20f;
        private const float BoosterGravitySeconds = 0.10f;
        private const int MaxSettlePasses = 100;

        private readonly RuleBook _rules;
        private readonly SpecialSpawnSettings _specialSpawn;
        private readonly List<GridPos> _matches = new List<GridPos>();

        public Match3Game(BoardState board, GameSession session, IRandomSource random,
            IReadOnlyList<IActionRule> rules, SpecialSpawnSettings specialSpawn)
        {
            Board = board;
            Session = session;
            Random = random;
            _rules = new RuleBook(rules ?? DefaultRules());
            _specialSpawn = specialSpawn;
        }

        public BoardState Board { get; }
        public GameSession Session { get; }
        public IRandomSource Random { get; }

        /// <summary>The rules every level uses today, in priority order.</summary>
        public static IActionRule[] DefaultRules() => new IActionRule[]
        {
            new LineBlastOnSwapRule(),
            new BoneDamageRule(),
            new PredatorsEatRule(),
        };

        /// <summary>A normal swap. Uses a move only when it makes a match or sets off a special.</summary>
        public TurnResult TrySwap(GridPos from, GridPos to, TurnContext context)
        {
            var reason = ValidateSwap(from, to);
            if (reason != RejectReason.None)
                return TurnResult.Rejected(reason);

            var run = NewRun();
            run.Enqueue(new SwapAction(from, to));
            run.ApplyNext(); // apply the swap only; specials it sets off wait in the queue

            bool setOffSpecial = run.HasPending;
            if (!setOffSpecial && !MatchFinder.HasAnyMatch(Board))
            {
                Board.Swap(from, to);
                run.Record(new SwapBackStep(from, to));
                return new TurnResult(TurnOutcome.NoMatch, RejectReason.None, run.Steps, false);
            }

            int movesLeft = context.MovesLeft - 1;
            run.Record(new MoveUsedStep(byMatch: !setOffSpecial));
            RollSpecialSpawn(movesMade: context.MaxMoves - movesLeft);

            run.Drain();
            if (setOffSpecial)
                FillGaps(run, CascadeGravitySeconds);

            bool won = ResolveUntilStable(run, CanShuffle(context.TimerActive, movesLeft));
            return Resolved(run, won);
        }

        /// <summary>A swap during the timer booster: no match needed, no move used, nothing resolves yet.</summary>
        public TurnResult SwapFree(GridPos from, GridPos to)
        {
            var reason = ValidateSwap(from, to);
            if (reason != RejectReason.None)
                return TurnResult.Rejected(reason);

            Board.Swap(from, to);
            return new TurnResult(TurnOutcome.Resolved, RejectReason.None, new TurnStep[] { new SwapStep(from, to) }, false);
        }

        /// <summary>Resolves whatever is on the board, for example when the timer booster ends.</summary>
        public TurnResult ResolveBoard(TurnContext context)
        {
            var run = NewRun();
            bool won = ResolveUntilStable(run, CanShuffle(context.TimerActive, context.MovesLeft));
            return Resolved(run, won);
        }

        /// <summary>Clears an area for a booster (the bomb). Uses no move.</summary>
        public TurnResult ClearCells(IReadOnlyList<GridPos> cells, TurnContext context)
        {
            var run = NewRun();
            run.Run(new ClearAction(cells, ClearCause.Booster, showPop: false));
            run.Run(new GravityRefillAction(BoosterGravitySeconds));
            bool won = ResolveUntilStable(run, CanShuffle(context.TimerActive, context.MovesLeft));
            return Resolved(run, won);
        }

        /// <summary>Shuffles the board if it has no legal move. Returns an empty result if it already has one.</summary>
        public TurnResult EnsurePlayable(TurnContext context)
        {
            var run = NewRun();
            if (CanShuffle(context.TimerActive, context.MovesLeft) && !MoveFinder.HasAnyMove(Board))
                run.Run(new ShuffleAction());
            return Resolved(run, false);
        }

        public RejectReason ValidateSwap(GridPos from, GridPos to)
        {
            var a = Board.DefinitionAt(from);
            var b = Board.DefinitionAt(to);
            if (a == null || b == null) return RejectReason.EmptyCell;
            if (!a.CanSwap || !b.CanSwap) return RejectReason.NotSwappable;
            if (!from.IsAdjacentTo(to)) return RejectReason.NotAdjacent;
            return RejectReason.None;
        }

        /// <summary>
        /// Cascades until nothing changes: clear matches and refill, then let settled-board rules act.
        /// Stops as soon as every goal is met. Returns true if the goals are complete.
        /// </summary>
        private bool ResolveUntilStable(TurnRun run, bool canShuffle)
        {
            for (int pass = 0; pass < MaxSettlePasses; pass++)
            {
                if (Session.AreAllGoalsComplete) return true;

                MatchFinder.Find(Board, _matches);
                while (_matches.Count > 0)
                {
                    run.Run(new ClearAction(_matches.ToArray(), ClearCause.Match, showPop: true));
                    run.Run(new GravityRefillAction(CascadeGravitySeconds));

                    if (Session.AreAllGoalsComplete) return true;
                    MatchFinder.Find(Board, _matches);
                }

                run.Run(new BoardSettledAction());

                // A settled-board rule may have opened gaps or lined up a match; if so, cascade again.
                if (!Board.HasEmptyCell() && !MatchFinder.HasAnyMatch(Board))
                    break;

                FillGaps(run, CascadeGravitySeconds);
            }

            if (Session.AreAllGoalsComplete) return true;

            if (canShuffle && !MoveFinder.HasAnyMove(Board))
                run.Run(new ShuffleAction());

            return false;
        }

        private void RollSpecialSpawn(int movesMade)
        {
            if (!_specialSpawn.Enabled || _specialSpawn.EveryMoves <= 0) return;
            if (movesMade % _specialSpawn.EveryMoves != 0) return;
            if (Board.ArmedSpawnType >= 0) return;

            float chance = _specialSpawn.Chance < 0f ? 0f : _specialSpawn.Chance > 1f ? 1f : _specialSpawn.Chance;
            if (Random.Value() < chance)
                Board.ArmedSpawnType = _specialSpawn.Type;
        }

        private static void FillGaps(TurnRun run, float seconds)
        {
            if (run.Board.HasEmptyCell())
                run.Run(new GravityRefillAction(seconds));
        }

        private static bool CanShuffle(bool timerActive, int movesLeft) => timerActive || movesLeft > 0;

        private TurnRun NewRun() => new TurnRun(Board, Session, Random, _rules);

        private static TurnResult Resolved(TurnRun run, bool goalsComplete) =>
            new TurnResult(TurnOutcome.Resolved, RejectReason.None, run.Steps, goalsComplete);
    }
}
