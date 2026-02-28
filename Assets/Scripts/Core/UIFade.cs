using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public static class UIFade
{
    // Fades a CanvasGroup to alpha=1 over duration of seconds (unscaled time).
    public static async Task FadeIn(CanvasGroup group, float duration, CancellationToken token = default)
    {
        if (!group) return;

        group.gameObject.SetActive(true);
        group.blocksRaycasts = true;
        group.interactable = true;

        float start = group.alpha;
        float t = 0f;

        while (t < duration)
        {
            token.ThrowIfCancellationRequested();
            await Task.Yield();
            t += Time.unscaledDeltaTime;

            float k = duration <= 0f ? 1f : Mathf.Clamp01(t / duration);
            group.alpha = Mathf.Lerp(start, 1f, k);
        }

        group.alpha = 1f;
    }
}