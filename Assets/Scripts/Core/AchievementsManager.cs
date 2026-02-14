using System.Collections.Generic;
using UnityEngine;

public class AchievementsManager : MonoBehaviour
{
    [SerializeField] private LevelCompletedEventChannelSO _levelCompletedChannel;
    [SerializeField] private AnimalsDestroyedEventChannelSO _animalDestroyedChannel;
    [SerializeField] private List<AchievementSO> _achievements;
    [SerializeField] private LevelsData _allLevels;

    private HashSet<string> _unlockedAchievements = new();
    private HashSet<int> _completedLevels = new();
    private HashSet<string> _discoveredAnimals = new();
    private Dictionary<string, int> _destroyedAnimals = new();

    private int _totalDestroyedAnimals = 0;
    private const int TotalAnimalTypes = 5;

    private void OnEnable()
    {
        _levelCompletedChannel.OnEventRaised += AddCompletedLevel;
        _animalDestroyedChannel.OnEventRaised += OnAnimalDestroyed;
    }

    private void OnDisable()
    {
        _levelCompletedChannel.OnEventRaised -= AddCompletedLevel;
        _animalDestroyedChannel.OnEventRaised -= OnAnimalDestroyed;
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
            if (achievement.Category != AchievementCategory.Level || _unlockedAchievements.Contains(achievement.Id)) continue; // skip if not a Level achievement or already unlocked
            bool unlock = false;

            if (achievement.Goal == 0) unlock = _completedLevels.Count >= _allLevels.Levels.Count; // finish all levels goal
            else unlock = _completedLevels.Count >= achievement.Goal; //  check if total unique completed levels are higher than required goal

            if (unlock)
            {
                _unlockedAchievements.Add(achievement.Id);
                Debug.Log($"Achievement unlocked: {achievement.Title}");
            }
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
        foreach (var achievement in _achievements)
        {
            if (achievement.Category != AchievementCategory.Animal || _unlockedAchievements.Contains(achievement.Id)) continue;
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

            if (unlock)
            {
                _unlockedAchievements.Add(achievement.Id);
                Debug.Log($"Achievement unlocked: {achievement.Title}");
            }
        }
    }

}
