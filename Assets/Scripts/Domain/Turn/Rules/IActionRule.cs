using System;

namespace FuzzyFrenzy.Domain
{
    /// <summary>
    /// A game rule (an animal ability, an obstacle behaviour). After each action of <see cref="ActionType"/>,
    /// the rule may react by queuing more actions. Rules never change the board themselves.
    /// Derive from <see cref="ActionRule{TAction}"/> rather than implementing this directly.
    /// </summary>
    public interface IActionRule
    {
        /// <summary>The action type this rule reacts to, including derived types. Null means every action.</summary>
        Type ActionType { get; }

        void React(TurnRun run, IBoardAction applied);
    }
}
