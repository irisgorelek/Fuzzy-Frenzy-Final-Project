using System;

/// <summary>Where a play of a level is. Every level moves Playing → Won or Playing → Lost, once.</summary>
public enum LevelPhase
{
    Playing,
    Won,
    Lost
}

/// <summary>
/// The level's state machine. Owns the one-way move to Won or Lost and the input lock that comes with it,
/// so a level can never be won twice, or won after it was lost.
/// </summary>
public sealed class LevelFlow : IDisposable
{
    private readonly InputGate _input;
    private IDisposable _levelOverLock;

    public LevelFlow(InputGate input)
    {
        _input = input;
    }

    public LevelPhase Phase { get; private set; } = LevelPhase.Playing;
    public bool IsOver => Phase != LevelPhase.Playing;

    /// <summary>Moves to <paramref name="end"/> if the level is still being played. False if it already ended.</summary>
    public bool TryEnd(LevelPhase end)
    {
        if (end == LevelPhase.Playing)
            throw new ArgumentException("A level can only end as Won or Lost.", nameof(end));
        if (IsOver)
            return false;

        Phase = end;
        _levelOverLock = _input.Acquire("level-over");
        return true;
    }

    /// <summary>Releases the level-over lock, for when the level is started again in the same scene.</summary>
    public void Dispose()
    {
        _levelOverLock?.Dispose();
        _levelOverLock = null;
    }
}
