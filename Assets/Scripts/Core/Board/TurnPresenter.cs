using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;
using UnityEngine;

/// <summary>Receives the gameplay side effects of a turn at the moment the player sees them.</summary>
public interface ITurnPresentationListener
{
    void OnMoveUsed();
    /// <summary>Tiles were scored. <paramref name="matched"/> counts toward "matches" goals.</summary>
    void OnTilesScored(int points, int matched);
    void OnTilesDestroyed(string tileId, int count);
    void OnBoneBroken(Vector2Int cell);
    void OnGoalProgressChanged();
}

/// <summary>
/// Replays a <see cref="TurnResult"/> on the board view, step by step. The core has already resolved the
/// whole turn; this only decides what the player sees and hears, and when.
/// </summary>
public sealed class TurnPresenter
{
    private const float SwapSeconds = 0.18f;
    private const float WolfNudgeSeconds = 0.20f;
    private const float PopVolume = 0.2f;
    private const float SwapVolume = 0.2f;

    // AudioManager SFX ids
    private const int SfxPop = 8;
    private const int SfxSwap = 12;
    private const int SfxSheepColumnBlast = 17;
    private const int SfxSheepRowBlast = 18;

    private readonly BoardView _view;
    private readonly AnimalCatalog _catalog;
    private readonly ITurnPresentationListener _listener;
    private readonly List<Board.FallMove> _falls = new List<Board.FallMove>();
    private readonly List<Board.SpawnInfo> _spawns = new List<Board.SpawnInfo>();

    public TurnPresenter(BoardView view, AnimalCatalog catalog, ITurnPresentationListener listener)
    {
        _view = view;
        _catalog = catalog;
        _listener = listener;
    }

    /// <summary>Plays every step in order. Returns false if it stopped early because the scene is closing.</summary>
    public async Task<bool> PlayAsync(TurnResult result, CancellationToken cancellation)
    {
        if (result == null) return true;

        foreach (var step in result.Steps)
        {
            if (cancellation.IsCancellationRequested || _view == null)
                return false;

            await PlayStepAsync(step);
        }

        return !cancellation.IsCancellationRequested;
    }

    private Task PlayStepAsync(TurnStep step)
    {
        switch (step)
        {
            case SwapStep s:
                PlaySfx(SfxSwap, SwapVolume);
                return _view.AnimateSwap(s.From.ToVector(), s.To.ToVector(), SwapSeconds);

            case SwapBackStep s:
                return _view.AnimateSwap(s.From.ToVector(), s.To.ToVector(), SwapSeconds);

            case MoveUsedStep s:
                if (s.ByMatch) PlaySfx(SfxPop, PopVolume);
                _listener.OnMoveUsed();
                return Task.CompletedTask;

            case MatchPopStep s:
                return _view.AnimateMatchPopFx(s.Cells.ToVectors(), BoardEffectsTiming.MatchPop);

            case ClearStep s:
                PlayClear(s);
                return Task.CompletedTask;

            case BonesBrokenStep s:
                PlayBonesBroken(s.Cells, s.BoneId);
                _listener.OnGoalProgressChanged();
                return Task.CompletedTask;

            case BlackSheepBlastStep s: return PlayBlackSheepBlastAsync(s);
            case GravityStep s: return PlayGravityAsync(s);
            case WolfFeastStep s: return PlayWolfFeastAsync(s);
            case ShuffleStep s: return PlayShuffleAsync(s);

            default:
                Debug.LogWarning($"TurnPresenter: no presentation for {step.GetType().Name}.");
                return Task.CompletedTask;
        }
    }

    private void PlayClear(ClearStep step)
    {
        if (step.Cause == ClearCause.Match)
            for (int i = 0; i < step.Cleared.Count; i++)
                PlaySfx(SfxPop, PopVolume);

        _listener.OnTilesScored(step.Points, step.Matched);

        for (int i = 0; i < step.Destroyed.Count; i++)
            _listener.OnTilesDestroyed(step.Destroyed[i].Id, step.Destroyed[i].Count);

        _listener.OnGoalProgressChanged();
    }

    private void PlayBonesBroken(IReadOnlyList<GridPos> cells, string boneId)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            _listener.OnBoneBroken(cells[i].ToVector());
            _listener.OnTilesDestroyed(boneId, 1);
        }
    }

    private async Task PlayBlackSheepBlastAsync(BlackSheepBlastStep step)
    {
        var center = step.Center.ToVector();
        bool vertical = step.SwipedVertically;

        await _view.AnimateSheepAnger(center);
        _view.PlayBlackSheepFx(center, vertical, _view._width, _view._height);
        PlaySfx(vertical ? SfxSheepRowBlast : SfxSheepColumnBlast);

        foreach (var wave in step.Waves)
        {
            if (wave.BonesBroken.Count > 0)
            {
                PlayBonesBroken(wave.BonesBroken, step.BoneId);
                _listener.OnGoalProgressChanged();
            }

            if (wave.PopCells.Count > 0)
                PlaySfx(SfxPop, PopVolume);

            await _view.AnimateMatchPopFx(wave.PopCells.ToVectors(), BoardEffectsTiming.LineStep, BoardClearEffect.BlackSheep);
        }

        await _view.WaitForBlackSheepParticles();
    }

    private async Task PlayGravityAsync(GravityStep step)
    {
        // Reused every cascade. Safe because steps play one at a time and the view only reads these lists
        // while building its animation, before the await returns.
        _falls.Clear();
        foreach (var f in step.Falls)
            _falls.Add(new Board.FallMove { from = f.From.ToVector(), to = f.To.ToVector() });

        _spawns.Clear();
        foreach (var s in step.Spawns)
            _spawns.Add(new Board.SpawnInfo { cell = s.Cell.ToVector(), animal = _catalog.AnimalOf(s.Type), spawnFromY = s.SpawnFromY });

        await _view.AnimateGravity(_falls, _spawns, new SnapshotAnimalGrid(step.After, _catalog), step.Duration);
        _listener.OnGoalProgressChanged();
    }

    private async Task PlayWolfFeastAsync(WolfFeastStep step)
    {
        _listener.OnTilesScored(step.Points, step.Eaten);
        if (step.Eaten > 0)
            _listener.OnTilesDestroyed(step.SheepId, step.Eaten);

        var bone = _catalog.AnimalOf(step.BoneType);
        foreach (var meal in step.Meals)
        {
            await _view.AnimateWolfNudge(meal.Wolf.ToVector(), meal.Sheep.ToVector(), WolfNudgeSeconds);
            _view.SetCellAnimal(meal.Sheep.ToVector(), bone);
            _listener.OnGoalProgressChanged();
        }
    }

    private async Task PlayShuffleAsync(ShuffleStep step)
    {
        await _view.ShowShuffleMessage(0.35f);
        await _view.AnimateShuffle(new SnapshotAnimalGrid(step.After, _catalog), 0.07f, 0.09f, 0.008f);
    }

    private static void PlaySfx(int id, float volume = 1f)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFXPitchAdjusted(id, volume);
    }
}
