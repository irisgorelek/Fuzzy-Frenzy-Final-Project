public sealed class BoardControllerTimerBombFlow
{
    private readonly BoardController _controller;

    public BoardControllerTimerBombFlow(BoardController controller)
    {
        _controller = controller;
    }

    public void Tick()
    {
        if (!_controller.TimerBomb.ShouldTick)
        {
            return;
        }

        _controller.TimerBomb.UpdateTimerUI(_controller.View);

        if (_controller.TimerBomb.ShouldEnd(_controller.AreAllGoalsComplete()))
        {
            EndTimerBomb();
        }
    }

    public void StartTimerBomb(float durationSeconds)
    {
        if (_controller.IsLevelOver) return;

        _controller.TimerBomb.Start(durationSeconds);

        _controller.OnTimerBombStateChanged?.Invoke(true);
        _controller.View.SetTimerVisible(true);
        _controller.TimerBomb.UpdateTimerUI(_controller.View);

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayTimerMusic();
            AudioManager.instance.PlaySFX(13);
        }

        _controller.View.SwapsEnabled = true;
    }

    private async void EndTimerBomb()
    {
        _controller.TimerBomb.BeginResolving();

        _controller.OnTimerBombStateChanged?.Invoke(false);

        _controller.View.SetTimerVisible(false);

        // freeze input while resolving
        _controller.View.SwapsEnabled = false;
        _controller.IsBusy = true;

        if (AudioManager.instance != null)
            AudioManager.instance.PlayBG((int)_controller.Cfg.songNumber);

        await _controller.ResolveCascadesAsync();

        if (!_controller.IsLevelOver)
        {
            if (_controller.TryHandleLevelFailed())
            {
                _controller.IsBusy = false;
                _controller.TimerBomb.FinishResolving();
                return;
            }
        }

        _controller.HideNormalBubbleIfActive();

        await _controller.TryShowTriggeredDialogueAsync();

        _controller.IsBusy = false;
        _controller.View.SwapsEnabled = !_controller.IsLevelOver;
        _controller.TimerBomb.FinishResolving();
    }
}
