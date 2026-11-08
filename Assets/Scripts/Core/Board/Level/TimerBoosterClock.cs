using System;
using UnityEngine;

/// <summary>
/// The timer booster's countdown and its on-screen timer. It only keeps time; what happens when time runs
/// out (resolving the board) is the controller's job.
/// </summary>
public sealed class TimerBoosterClock
{
    private readonly IBoardHud _hud;
    private readonly IGameAudio _audio;
    private float _endTime;
    private int _lastShownSecond = -1;

    public TimerBoosterClock(IBoardHud hud, IGameAudio audio)
    {
        _hud = hud;
        _audio = audio;
    }

    public bool IsActive { get; private set; }

    /// <summary>Raised with true when the booster starts and false when it stops.</summary>
    public event Action<bool> ActiveChanged;

    public void Start(float durationSeconds)
    {
        IsActive = true;
        _endTime = Time.time + durationSeconds;
        _lastShownSecond = -1;

        ActiveChanged?.Invoke(true);
        _hud.SetTimerVisible(true);
        RefreshDisplay();

        _audio.PlayTimerMusic();
        _audio.PlaySfx(SfxId.TimerTicks);
    }

    public void Stop()
    {
        if (!IsActive)
            return;

        IsActive = false;
        ActiveChanged?.Invoke(false);
        _hud.SetTimerVisible(false);
    }

    /// <summary>Updates the display. True once the time is up.</summary>
    public bool Tick()
    {
        if (!IsActive)
            return false;

        RefreshDisplay();
        return Time.time >= _endTime;
    }

    /// <summary>Shows or hides the timer to match the current state, for example when the scene is re-enabled.</summary>
    public void SyncDisplay()
    {
        _lastShownSecond = -1;
        _hud.SetTimerVisible(IsActive);
        if (IsActive)
            RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        int seconds = Mathf.CeilToInt(Mathf.Max(0f, _endTime - Time.time));
        if (seconds == _lastShownSecond)
            return;

        _lastShownSecond = seconds;
        _hud.SetTimerSeconds(seconds);
    }
}
