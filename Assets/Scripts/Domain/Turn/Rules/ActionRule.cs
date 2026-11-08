using System;

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
}
