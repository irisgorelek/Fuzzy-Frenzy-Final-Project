using System.Collections.Generic;
using UnityEngine;

internal sealed class FakeHud : IBoardHud
{
    public int Points, Goal;
    public void SetLevelNumber(int level) { }
    public void SetBackground(Sprite backgroundSprite) { }
    public void ShowGoal(bool show) { }
    public void SetScore(int points, int totalPoints) { Points = points; Goal = totalPoints; }
    public void SetMatchedAnimals(int animals, int goal) { }
    public void SetCollectGoals(List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected) { }
    public void SetPointsAndCollectGoals(int points, int pointsGoal, List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected) { }
    public void SetMovesText(int movesLeft) { }
    public void SetMovesLastMoveTension(bool active) { }
    public void SetTimerVisible(bool visible) { }
    public void SetTimerSeconds(int seconds) { }
}
