using System.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;

public class AsyncDelayTimer : MonoBehaviour
{
    private CancellationTokenSource _cts; // A cancel switch (stops the function after calling _cts.cancel)

    public void StartTimer(float seconds, Action onFinished)
    {
        StopTimer();
        _cts = new CancellationTokenSource();
        _ = Run(seconds, onFinished, _cts.Token); // fire-and-forget safely with cancellation (Using '_ =' means you intentionally don’t await it) 
    }

    public void StopTimer()
    {
        if (_cts == null) return;
        _cts.Cancel();
        _cts.Dispose();
        _cts = null;
    }

    private async Task Run(float seconds, Action onFinished, CancellationToken token)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(seconds), token);
            onFinished?.Invoke();
        }
        catch (OperationCanceledException)
        {
            // expected whenever the timer is reset/stopped
        }
    }

    private void OnDestroy() => StopTimer();
}