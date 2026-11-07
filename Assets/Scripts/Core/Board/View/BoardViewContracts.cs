using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// What replaying a turn needs from the board view: moving tiles and the effects that go with them.
/// <see cref="TurnPresenter"/> and the step players depend on this, not on the whole <see cref="BoardView"/>.
/// </summary>
public interface IBoardAnimator
{
    int Width { get; }
    int Height { get; }

    void SetCellAnimal(Vector2Int coord, Animal animal);

    Task AnimateSwap(Vector2Int a, Vector2Int b, float duration);
    Task AnimateGravity(List<Board.FallMove> moves, List<Board.SpawnInfo> spawns, IAnimalGrid board, float duration);
    Task AnimateWolfNudge(Vector2Int predatorCell, Vector2Int preyCell, float duration);
    Task ShowShuffleMessage(float hold);
    Task AnimateShuffle(IAnimalGrid board, float outDuration, float inDuration, float stagger);

    Task AnimateMatchPopFx(List<Vector2Int> matches, float duration, BoardClearEffect effect);
    Task AnimateSheepAnger(Vector2Int coord);
    void PlayBlackSheepFx(Vector2Int coord, bool horizontal, int width, int height);
    Task WaitForBlackSheepParticles();
}

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
