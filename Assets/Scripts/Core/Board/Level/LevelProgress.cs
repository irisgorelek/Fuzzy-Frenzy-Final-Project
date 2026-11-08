using System;
using FuzzyFrenzy.Domain;

/// <summary>
/// Goals, score and the end of the level. While a turn replays, the presenter reports progress here as the
/// player sees it; after the turn, <see cref="TurnRunner"/> asks whether the level was won or lost.
/// </summary>
public sealed class LevelProgress : ITurnPresentationListener, IDisposable
{
    private readonly BoardConfig _config;
    private readonly GameSession _session;
    private readonly MoveCounter _moves;
    private readonly LevelFlow _flow;
    private readonly GoalProgressTracker _goals;
    private readonly LevelOutcomePresenter _outcome;
    private readonly LevelScoreEventChannelSO _scoreChannel;
    private readonly AnimalsDestroyedEventChannelSO _destroyedChannel;

    public LevelProgress(BoardConfig config, GameSession session, MoveCounter moves, InputGate input, IBoardHud hud,
        LevelOutcomePresenter outcome, LevelScoreEventChannelSO scoreChannel, AnimalsDestroyedEventChannelSO destroyedChannel)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _moves = moves;
        _outcome = outcome;
        _scoreChannel = scoreChannel;
        _destroyedChannel = destroyedChannel;

        _flow = new LevelFlow(input);
        _goals = new GoalProgressTracker(config, hud);
    }

    public bool IsOver => _flow.IsOver;
    public LevelPhase Phase => _flow.Phase;

    /// <summary>Draws the goal counters as the player has seen them so far.</summary>
    public void Render() => _goals.Render();

    /// <summary>Ends the level as won if every goal is complete. True if it was won now.</summary>
    public bool TryWin()
    {
        if (!_session.AreAllGoalsComplete || !_flow.TryEnd(LevelPhase.Won))
            return false;

        _outcome.ShowWin(_config, _moves.MovesUsed, _session.Points);
        return true;
    }

    /// <summary>Ends the level as lost if the moves ran out with goals still open. True if the level is over.</summary>
    public bool TryLose()
    {
        if (IsOver) return true;
        if (_session.AreAllGoalsComplete) return false;
        if (_moves.MovesLeft > 0) return false;

        _flow.TryEnd(LevelPhase.Lost);
        _outcome.ShowLose();
        return true;
    }

    /// <summary>Debug: completes every goal and wins.</summary>
    public void ForceWin()
    {
        if (IsOver) return;

        _session.CheatCompleteGoals();
        _goals.SyncWith(_session);
        _goals.Render();
        TryWin();
    }

    /// <summary>Debug: loses now. False if the level had already ended.</summary>
    public bool ForceLose()
    {
        if (!_flow.TryEnd(LevelPhase.Lost))
            return false;

        _outcome.ShowLose();
        return true;
    }

    /// <summary>Releases the level-over input lock, for when the level restarts.</summary>
    public void Dispose() => _flow.Dispose();

    // ---- ITurnPresentationListener ----

    void ITurnPresentationListener.OnMoveUsed() => _moves.UseMove();

    void ITurnPresentationListener.OnTilesScored(int points, int matched)
    {
        _goals.AddScored(points, matched);

        if (points > 0 && _scoreChannel != null)
            _scoreChannel.RaiseEvent(points);
    }

    void ITurnPresentationListener.OnTilesDestroyed(string tileId, int count)
    {
        if (string.IsNullOrEmpty(tileId) || count <= 0)
            return;

        if (_destroyedChannel != null)
            _destroyedChannel.RaiseEvent(tileId, count);

        _goals.AddDestroyed(tileId, count);

        if (_goals.HasCollectGoals)
            _goals.Render();
    }

    void ITurnPresentationListener.OnGoalProgressChanged() => _goals.Render();
}
