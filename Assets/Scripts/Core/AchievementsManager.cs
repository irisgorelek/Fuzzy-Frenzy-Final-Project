using System.Collections.Generic;
using UnityEngine;

public class AchievementsManager : MonoBehaviour
{
    [SerializeField] private LevelCompletedEventChannelSO _levelCompletedChannel;
    [SerializeField] private AnimalsDestroyedEventChannelSO _animalDestroyedChannel;
    [SerializeField] private PowerUpEventChannelSO _powerUpChannel;
    [SerializeField] private ScoreEventChannelSO _scoreChannel;

    [SerializeField] private List<AchievementSO> _achievements;
    [SerializeField] private LevelsData _allLevels;

    private HashSet<string> _unlockedAchievements = new();
    private HashSet<int> _completedLevels = new();
    private HashSet<string> _discoveredAnimals = new();
    private Dictionary<string, int> _destroyedAnimals = new();

    private int _totalDestroyedAnimals = 0;
    private int _totalPointsEarned = 0;
    private const int TotalAnimalTypes = 5;

    private void OnEnable()
    {
        _levelCompletedChannel.OnEventRaised += OnLevelCompleted;
        _animalDestroyedChannel.OnEventRaised += OnAnimalDestroyed;
        _powerUpChannel.OnEventRaised += OnPowerUpUsed;
        _scoreChannel.OnEventRaised += OnAddedScore;
    }

    private void OnDisable()
    {
        _levelCompletedChannel.OnEventRaised -= OnLevelCompleted;
        _animalDestroyedChannel.OnEventRaised -= OnAnimalDestroyed;
        _powerUpChannel.OnEventRaised -= OnPowerUpUsed;
        _scoreChannel.OnEventRaised -= OnAddedScore;
    }

    private void OnLevelCompleted(int levelId)
    {
        _completedLevels.Add(levelId);

        CheckForLevelAchievement();
    }

    private void CheckForLevelAchievement()
    {
        foreach (var achievement in GetLocked(AchievementCategory.Level))
        {
            bool unlock;

            if (achievement.Goal == 0) unlock = _completedLevels.Count >= _allLevels.Levels.Count; // finish all levels goal
            else unlock = _completedLevels.Count >= achievement.Goal; // check if total unique completed levels are higher than required goal

            if (unlock) Unlock(achievement);
        }
    }

    private void OnAnimalDestroyed(string animalId, int amount)
    {
        if (_destroyedAnimals.ContainsKey(animalId)) _destroyedAnimals[animalId] += amount; // if the animal is already in dictionary add to the destroyed amount
        else _destroyedAnimals[animalId] = amount; // first time this animal type was destroyed

        _totalDestroyedAnimals += amount; // all animals destroyed amount
        _discoveredAnimals.Add(animalId); // unique animals discovered

        CheckForAnimalAchievements();
    }

    private void CheckForAnimalAchievements()
    {
        foreach (var achievement in GetLocked(AchievementCategory.Animal))
        {
            bool unlock = false;

            if (achievement.Goal == 0) unlock = _discoveredAnimals.Count >= TotalAnimalTypes; // discover all animals achievement
            else
            {
                if (!string.IsNullOrEmpty(achievement.AnimalId)) // check if it's animal specific
                {
                    if (_destroyedAnimals.TryGetValue(achievement.AnimalId, out int count))
                        unlock = count >= achievement.Goal;
                }
                else unlock = _totalDestroyedAnimals >= achievement.Goal; // general animals achievement
            }

            if (unlock) Unlock(achievement);
        }
    }

    private void OnAddedScore(int amount)
    {
        _totalPointsEarned += amount;
        CheckForScoreAchievements();
    }

    private void CheckForScoreAchievements()
    {
        foreach (var achievement in GetLocked(AchievementCategory.Score))
        {
            if (_totalPointsEarned >= achievement.Goal)
                Unlock(achievement);
        }
    }

    private void OnPowerUpUsed(string powerUpName)
    {
        foreach (var achievement in _achievements)
        {
            if (achievement.Category != AchievementCategory.PowerUp || _unlockedAchievements.Contains(achievement.Id)) continue;

            if (achievement.PowerUpName == powerUpName) Unlock(achievement);
        }
    }

    private void Unlock(AchievementSO achievement)
    {
        _unlockedAchievements.Add(achievement.Id);
        Debug.Log($"Achievement unlocked: {achievement.Title}");
    }

    private IEnumerable<AchievementSO> GetLocked(AchievementCategory category)
    {
        foreach (var achievement in _achievements)
        {
            if (achievement.Category != category) continue;
            if (_unlockedAchievements.Contains(achievement.Id)) continue;
            yield return achievement;
        }
    }

    public (int current, int goal) GetProgress(AchievementSO achievement)
    {
        if (_unlockedAchievements.Contains(achievement.Id))
        {
            int g = GetGoalValue(achievement);
            return (g, g);
        }

        switch (achievement.Category)
        {
            case AchievementCategory.Level:
                if (achievement.Goal == 0) // finish all levels
                    return (_completedLevels.Count, _allLevels.Levels.Count);

                return (_completedLevels.Count, achievement.Goal);

            case AchievementCategory.Animal:
                if (achievement.Goal == 0) // discover all animals
                    return (_discoveredAnimals.Count, TotalAnimalTypes);

                if (!string.IsNullOrEmpty(achievement.AnimalId))
                {
                    _destroyedAnimals.TryGetValue(achievement.AnimalId, out int count);
                    return (count, achievement.Goal);
                }

                return (_totalDestroyedAnimals, achievement.Goal);

            case AchievementCategory.PowerUp:
                return (0, 1); // bomb once (0 until unlocked)

            case AchievementCategory.Score:
                return (_totalPointsEarned, achievement.Goal);

            default:
                return (0, achievement.Goal);
        }
    }

    private int GetGoalValue(AchievementSO achievement)
    {
        switch (achievement.Category)
        {
            case AchievementCategory.Level:
                return achievement.Goal == 0 ? _allLevels.Levels.Count : achievement.Goal;

            case AchievementCategory.Animal:
                return achievement.Goal == 0 ? TotalAnimalTypes : achievement.Goal;

            case AchievementCategory.PowerUp:
                return 1;

            case AchievementCategory.Score:
                return achievement.Goal;

            default:
                return achievement.Goal;
        }
    }

    [ContextMenu("Log All Progress")]
    private void LogAllProgress()
    {
        foreach (var achievement in _achievements)
        {
            var (current, goal) = GetProgress(achievement);
            float percent = goal > 0 ? (float)current / goal * 100f : 0f;
            Debug.Log($"[{achievement.Category}] {achievement.Title}: {current}/{goal} ({percent:F0}%)");
        }
    }
}
