using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    public sealed class LevelGoals
    {
        public LevelGoals(GoalKind primary, int amount, IReadOnlyList<CollectGoal> collect)
        {
            Primary = primary;
            Amount = amount;
            Collect = collect ?? new List<CollectGoal>();
        }

        public GoalKind Primary { get; }
        public int Amount { get; }
        public IReadOnlyList<CollectGoal> Collect { get; }
        public bool HasCollectGoals => Collect.Count > 0;
    }
}
