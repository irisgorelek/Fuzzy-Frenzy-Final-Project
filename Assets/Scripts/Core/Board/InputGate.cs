using System;
using System.Collections.Generic;

/// <summary>
/// The one lock for player input. Anything that must not be interrupted (a turn playing out, a booster,
/// the end of the level) takes a named lock; input is open only while no locks are held.
/// </summary>
public sealed class InputGate
{
    private readonly List<string> _holders = new List<string>();

    /// <summary>Raised with the new open state whenever it changes.</summary>
    public event Action<bool> Changed;

    public bool IsOpen => _holders.Count == 0;

    /// <summary>Takes a lock. Dispose the result to release it. Several locks can be held at once.</summary>
    public IDisposable Acquire(string reason)
    {
        _holders.Add(reason ?? "unnamed");
        if (_holders.Count == 1)
            Changed?.Invoke(false);
        return new InputLock(this, reason ?? "unnamed");
    }

    public override string ToString() => IsOpen ? "open" : "locked by " + string.Join(", ", _holders);

    internal void Release(string reason)
    {
        if (_holders.Remove(reason) && _holders.Count == 0)
            Changed?.Invoke(true);
    }
}
