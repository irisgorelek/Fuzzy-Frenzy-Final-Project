using System;
using UnityEngine;

/// <summary>
/// What happens when a level ends: rewards, saving, the leaderboard, popups and music.
/// Knows nothing about turns or the board; the controller decides when a level ends.
/// </summary>
public sealed class LevelOutcomePresenter
{
    private readonly LevelOutcomeServices _services;

    public LevelOutcomePresenter(LevelOutcomeServices services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    public void ShowWin(BoardConfig config, int movesUsed, int finalScore)
    {
        DisableLevelVfx();

        int level = config.levelIndex;
        _services.LevelCompletedChannel?.RaiseEvent(level);

        int stars = _services.Rewards.GetStars(config.maxMoves, movesUsed);
        int coins = _services.Rewards.GetCoins(stars, level);

        SaveWin(level, stars, coins, finalScore);

        if (_services.ClearedDimmer != null)
            _services.ClearedDimmer.SetActive(true);

        if (_services.ClearedPopup != null)
            _services.ClearedPopup.Show(level, finalScore, coins, stars);
        else
            Debug.LogError("LevelClearedPopupUI is not assigned on BoardController.");

        var audio = _services.Audio ?? SilentAudio.Instance;
        audio.SetMusicVolume(0.4f);
        audio.PlaySfx(SfxId.Win);
        audio.SetMusicVolume(1f);
    }

    public void ShowLose()
    {
        DisableLevelVfx();

        var bootstrapper = Bootstrapper;
        if (bootstrapper != null)
            bootstrapper.Economy.TrySpendLifeOnLevelFail();

        if (_services.LostPopup != null)
            _services.LostPopup.Show();
        else
            Debug.LogError("LevelLostPopupUI is not assigned on BoardController.");
    }

    private GameBootstrapper Bootstrapper => _services.Locator != null ? _services.Locator.Bootstrapper : null;

    private void DisableLevelVfx()
    {
        if (_services.VfxToggle != null)
            _services.VfxToggle.SetCurrentVFXActive(false);
    }

    private void SaveWin(int level, int stars, int coins, int finalScore)
    {
        var bootstrapper = Bootstrapper;
        if (bootstrapper == null)
            return;

        bootstrapper.Economy.AddCoins(coins);

        var state = bootstrapper.Economy.State;

        // Save best star count per level
        state.levelStars.TryGetValue(level, out int bestStars);
        if (stars > bestStars)
            state.levelStars[level] = stars;

        // Save best score per level and recalculate total (match the displayed score formula)
        int displayScore = finalScore * 10;
        int shownScore = Mathf.RoundToInt(displayScore * (stars / 3f));
        state.TrySetLevelBestScore(level, shownScore);
        bootstrapper.Economy.Save();

        // Submit cumulative best to leaderboard
        var leaderboard = bootstrapper.Leaderboard;
        if (leaderboard != null && leaderboard.IsReady)
        {
            string displayName = string.IsNullOrEmpty(state.playerName) ? "Player" : state.playerName;
            _ = leaderboard.AddScore(state.playerId, displayName, state.totalPointsEarned);
        }
    }
}
