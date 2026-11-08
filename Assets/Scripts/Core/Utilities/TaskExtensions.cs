using System;
using System.Threading.Tasks;
using UnityEngine;

public static class TaskExtensions
{
    /// <summary>
    /// Runs a task from a Unity callback that can't await it, and logs any exception it throws.
    /// Use this instead of <c>async void</c>, whose exceptions are easy to lose.
    /// </summary>
    public static async void Forget(this Task task, UnityEngine.Object context = null)
    {
        try
        {
            await task;
        }
        catch (OperationCanceledException)
        {
            // The scene closed mid-animation; nothing to report.
        }
        catch (Exception e)
        {
            Debug.LogException(e, context);
        }
    }
}
