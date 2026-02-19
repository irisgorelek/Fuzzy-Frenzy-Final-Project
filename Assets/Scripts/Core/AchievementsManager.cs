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

    private EconomyContext _economy;
    private const int TotalAnimalTypes = 5;

    // Shortcut to state
    private PlayerEconomyState S => _economy.State;

    private void Start()
    {
        var bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        _economy = bootstrapper.Economy;
    }

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
        S.completedLevels.Add(levelId);
        _economy.Save();
        CheckForLevelAchievement();
    }

    private void CheckForLevelAchievement()
    {
        foreach (var achievement in GetLocked(AchievementCategory.Level))
        {
            bool unlock;

            if (achievement.Goal == 0) unlock = S.completedLevels.Count >= _allLevels.Levels.Count; // finish all levels goal
            else unlock = S.completedLevels.Count >= achievement.Goal; // check if total unique completed levels are higher than required goal

            if (unlock) Unlock(achievement);
        }
    }

    private void OnAnimalDestroyed(string animalId, int amount)
    {
        if (S.destroyedAnimals.ContainsKey(animalId)) S.destroyedAnimals[animalId] += amount; // if the animal is already in dictionary add to the destroyed amount
        else S.destroyedAnimals[animalId] = amount; // first time this animal type was destroyed

        S.totalDestroyedAnimals += amount; // all animals destroyed amount
        S.discoveredAnimals.Add(animalId); // unique animals discovered
        _economy.Save();
        CheckForAnimalAchievements();
    }

    private void CheckForAnimalAchievements()
    {
        foreach (var achievement in GetLocked(AchievementCategory.Animal))
        {
            bool unlock = false;

            if (achievement.Goal == 0) unlock = S.discoveredAnimals.Count >= TotalAnimalTypes; // discover all animals achievement
            else
            {
                if (!string.IsNullOrEmpty(achievement.AnimalId)) // check if it's animal specific
                {
                    if (S.destroyedAnimals.TryGetValue(achievement.AnimalId, out int count))
                        unlock = count >= achievement.Goal;
                }
                else unlock = S.totalDestroyedAnimals >= achievement.Goal; // general animals achievement
            }

            if (unlock) Unlock(achievement);
        }
    }

    private void OnAddedScore(int amount)
    {
        S.totalPointsEarned += amount;
        _economy.Save();
        CheckForScoreAchievements();
    }

    private void CheckForScoreAchievements()
    {
        foreach (var achievement in GetLocked(AchievementCategory.Score))
        {
            if (S.totalPointsEarned >= achievement.Goal)
                Unlock(achievement);
        }
    }

    private void OnPowerUpUsed(string powerUpName)
    {
        foreach (var achievement in GetLocked(AchievementCategory.PowerUp))
        {
            if (achievement.PowerUpName == powerUpName) Unlock(achievement);
        }
    }

    private void Unlock(AchievementSO achievement)
    {
        S.unlockedAchievements.Add(achievement.Id);
        _economy.Save();
        Debug.Log($"Achievement unlocked: {achievement.Title}");
    }

    private IEnumerable<AchievementSO> GetLocked(AchievementCategory category)
    {
        foreach (var achievement in _achievements)
        {
            if (achievement.Category != category) continue;
            if (S.unlockedAchievements.Contains(achievement.Id)) continue;
            yield return achievement;
        }
    }

    public (int current, int goal) GetProgress(AchievementSO achievement)
    {
        if (S.unlockedAchievements.Contains(achievement.Id))
        {
            int g = GetGoalValue(achievement);
            return (g, g);
        }

        return achievement.Category switch
        {
            AchievementCategory.Level => (S.completedLevels.Count, GetGoalValue(achievement)),
            AchievementCategory.Animal when achievement.Goal == 0 => (S.discoveredAnimals.Count, TotalAnimalTypes), // discover all animals
            AchievementCategory.Animal when !string.IsNullOrEmpty(achievement.AnimalId) =>
                (S.destroyedAnimals.TryGetValue(achievement.AnimalId, out int count) ? count : 0, achievement.Goal),
            AchievementCategory.Animal => (S.totalDestroyedAnimals, achievement.Goal),
            AchievementCategory.PowerUp => (0, 1), // bomb once (0 until unlocked)
            AchievementCategory.Score => (S.totalPointsEarned, achievement.Goal),
            _ => (0, achievement.Goal)
        };
    }

    private int GetGoalValue(AchievementSO achievement) => achievement.Category switch
    {
        AchievementCategory.Level => achievement.Goal == 0 ? _allLevels.Levels.Count : achievement.Goal,
        AchievementCategory.Animal => achievement.Goal == 0 ? TotalAnimalTypes : achievement.Goal,
        AchievementCategory.PowerUp => 1,
        _ => achievement.Goal
    };

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