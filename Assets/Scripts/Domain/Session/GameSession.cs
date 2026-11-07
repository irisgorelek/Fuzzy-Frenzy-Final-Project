using System.Collections.Generic;

namespace FuzzyFrenzy.Domain
{
    /// <summary>The level's main goal. Values match the order of the BoardConfig enum.</summary>
    public enum GoalKind
    {
        Points = 0,
        Matches = 1,
        CollectAnimals = 2
    }

    public readonly struct CollectGoal
    {
        public readonly string TileId;
        public readonly int Amount;

        public CollectGoal(string tileId, int amount)
        {
            TileId = tileId;
            Amount = amount;
        }
    }

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

    /// <summary>Score and goal progress for one play of a level. Never stored in a ScriptableObject.</summary>
    public sealed class GameSession
    {
        private readonly Dictionary<string, int> _collected = new Dictionary<string, int>();

        public GameSession(LevelGoals goals)
        {
            Goals = goals ?? new LevelGoals(GoalKind.Points, 0, null);
        }

        public LevelGoals Goals { get; }
        public int Points { get; private set; }
        public int MatchedAnimals { get; private set; }
        public IReadOnlyDictionary<string, int> Collected => _collected;

        public void AddPoints(int amount) => Points += amount;

        public void AddMatched(int count) => MatchedAnimals += count;

        public void AddDestroyed(string tileId, int count)
        {
            if (tileId == null || count <= 0) return;
            _collected.TryGetValue(tileId, out int have);
            _collected[tileId] = have + count;
        }

        public int CollectedCount(string tileId) =>
            tileId != null && _collected.TryGetValue(tileId, out int have) ? have : 0;

        /// <summary>The points or matches goal. A collect level counts matches here, as before.</summary>
        public bool IsPrimaryGoalReached =>
            Goals.Primary == GoalKind.Points ? Points >= Goals.Amount : MatchedAnimals >= Goals.Amount;

        public bool IsCollectComplete
        {
            get
            {
                var goals = Goals.Collect;
                for (int i = 0; i < goals.Count; i++)
                    if (CollectedCount(goals[i].TileId) < goals[i].Amount) return false;
                return true;
            }
        }

        public bool AreAllGoalsComplete
        {
            get
            {
                bool primary = Goals.Primary == GoalKind.CollectAnimals ? IsCollectComplete : IsPrimaryGoalReached;
                bool collect = !Goals.HasCollectGoals || IsCollectComplete;
                return primary && collect;
            }
        }

        /// <summary>Debug only: completes every goal.</summary>
        public void CheatCompleteGoals()
        {
            if (Goals.Primary == GoalKind.Points && Points < Goals.Amount) Points = Goals.Amount;
            else if (Goals.Primary == GoalKind.Matches && MatchedAnimals < Goals.Amount) MatchedAnimals = Goals.Amount;

            for (int i = 0; i < Goals.Collect.Count; i++)
            {
                var goal = Goals.Collect[i];
                if (goal.TileId != null) _collected[goal.TileId] = goal.Amount;
            }
        }
    }
}
