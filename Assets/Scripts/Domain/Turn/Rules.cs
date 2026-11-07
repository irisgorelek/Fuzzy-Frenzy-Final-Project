using System;
using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// Base for rules that react to one kind of action. The turn only calls a rule for actions of its type
    /// (or a type derived from it), so rules don't type-check every action themselves.
    /// </summary>
    public abstract class ActionRule<TAction> : IActionRule where TAction : IBoardAction
    {
        public Type ActionType => typeof(TAction);

        public void React(TurnRun run, IBoardAction applied) => React(run, (TAction)applied);

        protected abstract void React(TurnRun run, TAction applied);
    }

    /// <summary>Swapping a tile with <see cref="TileTraits.LineBlastOnSwap"/> sets off its blast where it ends up.</summary>
    public sealed class LineBlastOnSwapRule : ActionRule<SwapAction>
    {
        protected override void React(TurnRun run, SwapAction swap)
        {
            // After the swap, the tile the player dragged is at To and the other tile is at From.
            GridPos center;
            if (run.Board.Has(swap.To, TileTraits.LineBlastOnSwap)) center = swap.To;
            else if (run.Board.Has(swap.From, TileTraits.LineBlastOnSwap)) center = swap.From;
            else return;

            bool swipedVertically = swap.From.X == swap.To.X;
            run.Enqueue(new BlackSheepBlastAction(center, swipedVertically));
        }
    }

    /// <summary>Tiles with <see cref="TileTraits.BreaksNextToClear"/> (bones) break when a tile next to them is cleared.</summary>
    public sealed class BoneDamageRule : ActionRule<ClearAction>
    {
        private readonly List<GridPos> _buffer = new List<GridPos>();

        protected override void React(TurnRun run, ClearAction clear)
        {
            BoardOperations.FindBreakableNeighbours(run.Board, clear.DamageSources, _buffer);
            if (_buffer.Count > 0)
                run.Enqueue(new BreakBonesAction(_buffer.ToArray()));
        }
    }

    /// <summary>
    /// Once the board settles, every predator eats the prey next to it (see <see cref="TileDefinition.EatsId"/>).
    /// The wolf eating sheep and leaving bones is one case; new predators only need data.
    /// </summary>
    public sealed class PredatorsEatRule : ActionRule<BoardSettledAction>
    {
        private readonly List<Meal> _buffer = new List<Meal>();

        protected override void React(TurnRun run, BoardSettledAction applied)
        {
            BoardOperations.FindMeals(run.Board, _buffer);
            if (_buffer.Count > 0)
                run.Enqueue(new FeastAction(_buffer.ToArray()));
        }
    }

    /// <summary>
    /// The rules a game runs, indexed by action type. Built once per game; each action type's list keeps the
    /// rules' priority order.
    /// </summary>
    public sealed class RuleBook
    {
        private static readonly IActionRule[] None = new IActionRule[0];

        private readonly IReadOnlyList<IActionRule> _rules;
        private readonly Dictionary<Type, IActionRule[]> _byActionType = new Dictionary<Type, IActionRule[]>();

        public RuleBook(IReadOnlyList<IActionRule> rules)
        {
            _rules = rules ?? None;
        }

        /// <summary>The rules that react to this action, in priority order.</summary>
        public IActionRule[] For(IBoardAction action)
        {
            var type = action.GetType();
            if (_byActionType.TryGetValue(type, out var cached))
                return cached;

            var matching = new List<IActionRule>();
            for (int i = 0; i < _rules.Count; i++)
            {
                var wanted = _rules[i].ActionType ?? typeof(IBoardAction);
                if (wanted.IsAssignableFrom(type))
                    matching.Add(_rules[i]);
            }

            var result = matching.Count == 0 ? None : matching.ToArray();
            _byActionType[type] = result;
            return result;
        }
    }
}
