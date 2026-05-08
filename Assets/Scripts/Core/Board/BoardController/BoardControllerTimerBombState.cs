using UnityEngine;

public class BoardControllerTimerBombState
{
    public bool IsActive { get; private set; }
    public bool IsResolving { get; private set; }

    private float _endTime;
    private int _lastShownSecond = -1;

    public void ResetLastShownSecond()
    {
        _lastShownSecond = -1;
    }

    public void Start(float durationSeconds)
    {
        IsActive = true;
        IsResolving = false;
        _endTime = Time.time + durationSeconds;
        _lastShownSecond = -1;
    }

    public void Stop()
    {
        IsActive = false;
        IsResolving = false;
        _lastShownSecond = -1;
    }

    public void BeginResolving()
    {
        IsResolving = true;
        IsActive = false;
    }

    public void FinishResolving()
    {
        IsResolving = false;
    }

    public bool ShouldTick => IsActive && !IsResolving;

    public bool ShouldEnd(bool allGoalsComplete)
    {
        return Time.time >= _endTime || allGoalsComplete;
    }

    public void UpdateTimerUI(BoardView view)
    {
        float remaining = Mathf.Max(0f, _endTime - Time.time);
        int seconds = Mathf.CeilToInt(remaining);

        if (seconds == _lastShownSecond)
            return;

        _lastShownSecond = seconds;
        view.SetTimerSeconds(seconds);
    }
}
