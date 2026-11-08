using System.Collections.Generic;
using UnityEngine;

/// <summary>The level's heads-up display: goals, moves, the timer, the level number and background.</summary>
public interface IBoardHud
{
    void SetLevelNumber(int level);
    void SetBackground(Sprite backgroundSprite);

    void ShowGoal(bool show);
    void SetScore(int points, int totalPoints);
    void SetMatchedAnimals(int animals, int goal);
    void SetCollectGoals(List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected);
    void SetPointsAndCollectGoals(int points, int pointsGoal, List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected);

    void SetMovesText(int movesLeft);
    void SetMovesLastMoveTension(bool active);

    void SetTimerVisible(bool visible);
    void SetTimerSeconds(int seconds);
}
