using System;

/// <summary>One held input lock. Disposing it releases the lock once; later calls do nothing.</summary>
internal sealed class InputLock : IDisposable
{
    private InputGate _gate;
    private readonly string _reason;

    public InputLock(InputGate gate, string reason)
    {
        _gate = gate;
        _reason = reason;
    }

    public void Dispose()
    {
        _gate?.Release(_reason);
        _gate = null;
    }
}
