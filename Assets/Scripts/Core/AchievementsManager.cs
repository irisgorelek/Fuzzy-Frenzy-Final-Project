using System.Collections.Generic;
using UnityEngine;

public class AchievementsManager : MonoBehaviour
{
    [SerializeField] private LevelCompletedEventChannelSO _levelCompletedChannel;
    [SerializeField] private List<AchievementSO> _achievements;
    [SerializeField] private LevelsData _allLevels;

    private HashSet<string> unlockedAchievements = new();
    private HashSet<int> _completedLevels = new();

    private void OnEnable()
    {
        _levelCompletedChannel.OnEventRaised += AddCompletedLevel;
    }

    private void OnDisable()
    {
        _levelCompletedChannel.OnEventRaised -= AddCompletedLevel;
    }

    private void AddCompletedLevel(int levelId)
    {
        _completedLevels.Add(levelId);

        CheckForLevelAchievement();
    }

    private void CheckForLevelAchievement()
    {
        foreach (var achievement in _achievements)
        {
            if (achievement.Category != AchievementCategory.Level || unlockedAchievements.Contains(achievement.Id)) continue;
            bool unlock = false;

            if (achievement.Goal == 0) unlock = _completedLevels.Count >= _allLevels.Levels.Count; // finish all levels goal
            else unlock = _completedLevels.Count >= achievement.Goal;

            if (unlock)
            {
                unlockedAchievements.Add(achievement.Id);
                Debug.Log($"Achievement unlocked: {achievement.Title}");
            }
        }
    }
}
