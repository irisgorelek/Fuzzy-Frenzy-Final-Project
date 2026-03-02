using System.Collections.Generic;
using UnityEngine;

public class AchievementsUI : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private AchievementCardData cardPrefab;
    [SerializeField] private List<AchievementSO> achievements;
    [SerializeField] private LevelsData allLevels;

    private const int TotalAnimalTypes = 5;
    private PlayerEconomyState _state;

    private void Start()
    {
        _state = FindFirstObjectByType<GameBootstrapper>().Economy.State;
        LoadAchievements();
    }

    private void LoadAchievements()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (var achievement in achievements)
        {
            var (current, goal) = GetProgress(achievement);
            var card = Instantiate(cardPrefab, content);
            card.SetData(achievement, current, goal);
        }
    }

    private (int current, int goal) GetProgress(AchievementSO achievement)
    {
        if (_state.unlockedAchievements.Contains(achievement.Id))
        {
            int g = GetGoalValue(achievement);
            return (g, g);
        }

        return achievement.Category switch
        {
            AchievementCategory.Level => (_state.completedLevels.Count, GetGoalValue(achievement)),
            AchievementCategory.Animal when achievement.Goal == 0 => (_state.discoveredAnimals.Count, TotalAnimalTypes),
            AchievementCategory.Animal when !string.IsNullOrEmpty(achievement.AnimalId) =>
                (_state.destroyedAnimals.TryGetValue(achievement.AnimalId, out int count) ? count : 0, achievement.Goal),
            AchievementCategory.Animal => (_state.totalDestroyedAnimals, achievement.Goal),
            AchievementCategory.PowerUp => (0, 1),
            AchievementCategory.Score => (_state.totalPointsEarned, achievement.Goal),
            _ => (0, achievement.Goal)
        };
    }

    private int GetGoalValue(AchievementSO achievement) => achievement.Category switch
    {
        AchievementCategory.Level => achievement.Goal == 0 ? allLevels.Levels.Count : achievement.Goal,
        AchievementCategory.Animal => achievement.Goal == 0 ? TotalAnimalTypes : achievement.Goal,
        AchievementCategory.PowerUp => 1,
        _ => achievement.Goal
    };
}
