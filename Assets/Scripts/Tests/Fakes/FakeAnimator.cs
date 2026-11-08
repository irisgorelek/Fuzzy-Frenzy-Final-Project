using System.Collections.Generic;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;
using UnityEngine;

internal sealed class FakeAnimator : IBoardAnimator
{
    public int Width => 3;
    public int Height => 3;
    public void SetCellAnimal(Vector2Int coord, Animal animal) { }
    public Task AnimateSwap(Vector2Int a, Vector2Int b, float duration) => Task.CompletedTask;
    public Task AnimateGravity(IReadOnlyList<FallMove> falls, IReadOnlyList<SpawnedTile> spawns, IAnimalGrid after, float duration) => Task.CompletedTask;
    public Task AnimatePredatorNudge(Vector2Int predatorCell, Vector2Int preyCell, float duration) => Task.CompletedTask;
    public Task ShowShuffleMessage(float hold) => Task.CompletedTask;
    public Task AnimateShuffle(IAnimalGrid board, float outDuration, float inDuration, float stagger) => Task.CompletedTask;
    public Task AnimateMatchPopFx(List<Vector2Int> matches, float duration, BoardClearEffect effect) => Task.CompletedTask;
    public Task AnimateLineBlastCharge(Vector2Int coord) => Task.CompletedTask;
    public void PlayLineBlastFx(Vector2Int coord, bool horizontal, int width, int height) { }
    public Task WaitForLineBlastFx() => Task.CompletedTask;
    public void PlayTileBreakFx(Vector2Int coord) { }
}
