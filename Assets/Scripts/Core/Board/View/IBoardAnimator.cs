using System.Collections.Generic;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;
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
    /// <summary>Drops falling tiles and new tiles into place. <paramref name="after"/> is the board once they land.</summary>
    Task AnimateGravity(IReadOnlyList<FallMove> falls, IReadOnlyList<SpawnedTile> spawns, IAnimalGrid after, float duration);
    Task AnimatePredatorNudge(Vector2Int predatorCell, Vector2Int preyCell, float duration);
    Task ShowShuffleMessage(float hold);
    Task AnimateShuffle(IAnimalGrid board, float outDuration, float inDuration, float stagger);

    Task AnimateMatchPopFx(List<Vector2Int> matches, float duration, BoardClearEffect effect);
    Task AnimateLineBlastCharge(Vector2Int coord);
    void PlayLineBlastFx(Vector2Int coord, bool horizontal, int width, int height);
    Task WaitForLineBlastFx();

    /// <summary>The effect for an obstacle tile breaking.</summary>
    void PlayTileBreakFx(Vector2Int coord);
}
