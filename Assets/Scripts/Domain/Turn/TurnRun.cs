using System;
using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>Something that happens to the board. Applying it changes the board and records a step.</summary>
    public interface IBoardAction
    {
        void Apply(TurnRun run);
    }

    /// <summary>
    /// A game rule (an animal ability, an obstacle behaviour). After every action, each rule may react
    /// by queuing more actions. Rules never change the board themselves.
    /// </summary>
    public interface IActionRule
    {
        void React(TurnRun run, IBoardAction applied);
    }

    /// <summary>
    /// One turn's action queue. Takes one action at a time, applies it, records what happened,
    /// and lets every rule react. Only actions in this queue change the board.
    /// </summary>
    public sealed class TurnRun
    {
        /// <summary>Guards against two rules setting each other off forever.</summary>
        public const int MaxActionsPerTurn = 10000;

        private readonly Queue<IBoardAction> _queue = new Queue<IBoardAction>();
        private readonly List<TurnStep> _steps = new List<TurnStep>();
        private readonly IReadOnlyList<IActionRule> _rules;
        private int _applied;

        internal TurnRun(BoardState board, GameSession session, IRandomSource random, IReadOnlyList<IActionRule> rules)
        {
            Board = board;
            Session = session;
            Random = random;
            _rules = rules;
        }

        public BoardState Board { get; }
        public GameSession Session { get; }
        public IRandomSource Random { get; }

        internal IReadOnlyList<TurnStep> Steps => _steps;
        internal bool HasPending => _queue.Count > 0;

        public void Enqueue(IBoardAction action)
        {
            if (action != null) _queue.Enqueue(action);
        }

        public void Record(TurnStep step)
        {
            if (step != null) _steps.Add(step);
        }

        /// <summary>Applies the next action and lets every rule react to it.</summary>
        internal void ApplyNext()
        {
            if (++_applied > MaxActionsPerTurn)
                throw new InvalidOperationException($"A turn applied more than {MaxActionsPerTurn} actions. Two rules are probably triggering each other.");

            var action = _queue.Dequeue();
            action.Apply(this);

            for (int i = 0; i < _rules.Count; i++)
                _rules[i].React(this, action);
        }

        /// <summary>Applies actions until the queue is empty.</summary>
        internal void Drain()
        {
            while (_queue.Count > 0)
                ApplyNext();
        }

        internal void Run(IBoardAction action)
        {
            Enqueue(action);
            Drain();
        }
    }
}
