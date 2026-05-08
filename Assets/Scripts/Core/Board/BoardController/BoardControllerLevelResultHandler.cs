using UnityEngine;

public sealed class BoardControllerLevelResultHandler
{
    private readonly BoardController _controller;

    public BoardControllerLevelResultHandler(BoardController controller)
    {
        _controller = controller;
    }

    //private async Task<bool> TryHandleLevelCompleteAsync()
    public bool TryHandleLevelComplete()
    {
        if (_controller.IsLevelOver || !_controller.AreAllGoalsComplete())
            return false;

        _controller.IsLevelOver = true;

        if (_controller.LevelVFXToggle != null)
            _controller.LevelVFXToggle.SetCurrentVFXActive(false);

        _controller.LevelCompletedChannelSO?.RaiseEvent(_controller.Cfg.levelIndex);

        int movesUsed = _controller.MoveCounter.MovesUsed;
        int stars = _controller.Rewards.GetStars(_controller.Cfg.maxMoves, movesUsed);
        int coins = _controller.Rewards.GetCoins(stars, _controller.Cfg.levelIndex);
        int finalScore = _controller.Board.CurrentPoints;
        int level = _controller.Cfg.levelIndex;

        if (_controller.Locator != null && _controller.Locator.Bootstrapper != null)
        {
            _controller.Locator.Bootstrapper.Economy.AddCoins(coins);

            var state = _controller.Locator.Bootstrapper.Economy.State;

            // Save best star count per level
            state.levelStars.TryGetValue(level, out int bestStars);
            if (stars > bestStars)
                state.levelStars[level] = stars;

            // Save best score per level and recalculate total (match the displayed score formula)
            int displayScore = finalScore * 10;
            int shownScore = Mathf.RoundToInt(displayScore * (stars / 3f));
            state.TrySetLevelBestScore(level, shownScore);
            _controller.Locator.Bootstrapper.Economy.Save();

            // Submit cumulative best to leaderboard
            var leaderboard = _controller.Locator.Bootstrapper.Leaderboard;
            if (leaderboard != null && leaderboard.IsReady)
            {
                string displayName = string.IsNullOrEmpty(state.playerName) ? "Player" : state.playerName;
                _ = leaderboard.AddScore(state.playerId, displayName, state.totalPointsEarned);
            }
        }

        if (_controller.LevelClearedDimmer != null)
        {
            _controller.LevelClearedDimmer.SetActive(true);
        }

        if (_controller.LevelClearedPopupUI != null)
        {
            _controller.LevelClearedPopupUI.Show(level, finalScore, coins, stars);
        }
        else
        {
            Debug.LogError("LevelClearedPopupUI is not assigned on BoardController.");
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.ChangeMusicVolume(0.4f);
            AudioManager.instance.PlaySFX(16);
            //await WaitFrames(25);
            AudioManager.instance.ChangeMusicVolume(1f);
        }

        return true;
    }

    public bool TryHandleLevelFailed()
    {
        if (_controller.IsLevelOver) return true;
        if (_controller.AreAllGoalsComplete()) return false;
        if (_controller.MoveCounter.MovesLeft > 0) return false;

        ShowLosePopupAndSpendLife();
        return true;
    }

    public void DebugForceLose()
    {
        if (_controller.IsLevelOver)
            return;

        ShowLosePopupAndSpendLife();
        _controller.TimerBomb.Stop();
        _controller.View.SetTimerVisible(false);
    }

    private void ShowLosePopupAndSpendLife()
    {
        _controller.IsLevelOver = true;
        _controller.View.SwapsEnabled = false;

        if (_controller.LevelVFXToggle != null)
            _controller.LevelVFXToggle.SetCurrentVFXActive(false);

        if (_controller.Locator != null && _controller.Locator.Bootstrapper != null)
            _controller.Locator.Bootstrapper.Economy.TrySpendLifeOnLevelFail();

        if (_controller.LevelLostPopupUI != null)
            _controller.LevelLostPopupUI.Show();
        else
            Debug.LogError("LevelLostPopupUI is not assigned on BoardController.");
    }
}
