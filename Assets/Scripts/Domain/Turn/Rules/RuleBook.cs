using System;
using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
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
