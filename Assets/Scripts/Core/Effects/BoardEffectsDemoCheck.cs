#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>Optional Play-mode presentation check, created only by the editor menu.</summary>
public sealed class BoardEffectsDemoCheck : MonoBehaviour
{
    private IEnumerator Start()
    {
        string output = ".utmp/BoardEffectsPreview";
        Directory.CreateDirectory(output);
        var view = FindFirstObjectByType<BoardView>();
        var controller = FindFirstObjectByType<BoardController>();
        if (view == null || controller == null || controller.CurrentBoard == null)
            throw new InvalidOperationException("Enter Play mode on the Level scene first.");
        bool swapsEnabled = view.SwapsEnabled;
        view.SwapsEnabled = false;
        try
        {
            int y = controller.GetHeight() / 2;
            int x = controller.GetWidth() / 2;
            var center = new Vector2Int(x, y);
            var matches = new List<Vector2Int> {center + Vector2Int.left, center, center + Vector2Int.right};
            var matchTask = view.AnimateMatchPopFx(matches);
            yield return new WaitForSeconds(0.14f);
            RequireParticles("Match");
            yield return Capture(output, "LiveMatch");
            yield return new WaitForSeconds(0.95f);
            if (!matchTask.IsCompletedSuccessfully) throw new InvalidOperationException("Match tween did not complete.");
            RequireCleared();
            int firstPoolSize = view.GetFxParent().GetComponentsInChildren<BoardParticleEffect>(true).Length;
            view.AnimateMatchPopFx(matches);
            yield return new WaitForSeconds(1f);
            RequireCleared();
            int secondPoolSize = view.GetFxParent().GetComponentsInChildren<BoardParticleEffect>(true).Length;
            if (firstPoolSize != secondPoolSize) throw new InvalidOperationException("Repeated matches did not reuse their instances.");
            view.PlayBoneBreakFx(center);
            yield return new WaitForSeconds(0.14f);
            RequireParticles("Bones");
            yield return Capture(output, "LiveBones");
            yield return new WaitForSeconds(0.9f);
            RequireCleared();
            view.PlayBlackSheepFx(center, true, controller.GetWidth(), controller.GetHeight());
            yield return new WaitForSeconds(0.12f);
            RequireParticles("Horizontal line");
            yield return Capture(output, "LiveLineHorizontal");
            yield return new WaitForSeconds(1.1f);
            RequireCleared();
            view.PlayBlackSheepFx(center, false, controller.GetWidth(), controller.GetHeight());
            yield return new WaitForSeconds(0.14f);
            RequireParticles("Vertical line");
            yield return Capture(output, "LiveLineVertical");
            yield return new WaitForSeconds(1.1f);
            RequireCleared();
            if (!view.PlayBombBurstFx(center)) throw new InvalidOperationException("Bomb prefab is not assigned.");
            yield return new WaitForSeconds(0.14f);
            RequireParticles("Bomb");
            yield return Capture(output, "LiveBomb");
            yield return new WaitForSeconds(1f);
            RequireCleared();
            File.WriteAllText(output + "/RuntimeCheck.txt", "PASS: match, bones, horizontal line, vertical line, bomb; cleanup after every effect; repeated matches reuse instances.");
            Debug.Log("Board effects runtime check PASSED: five presentations, cleanup, and pooling.");
        }
        finally
        {
            view.SwapsEnabled = swapsEnabled;
            Destroy(gameObject);
        }
    }

    private static IEnumerator Capture(string output, string name)
    {
        yield return new WaitForEndOfFrame();
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        File.WriteAllBytes(output + "/" + name + ".png", texture.EncodeToPNG());
        Destroy(texture);
    }

    private static void RequireParticles(string effect)
    {
        int particles = 0;
        foreach (var instance in FindObjectsByType<BoardParticleEffect>(FindObjectsSortMode.None))
            particles += instance.LiveParticleCount;
        if (particles == 0) throw new InvalidOperationException(effect + " did not emit particles.");
    }

    private static void RequireCleared()
    {
        if (FindObjectsByType<BoardParticleEffect>(FindObjectsSortMode.None).Length > 0)
            throw new InvalidOperationException("An effect stayed active after its particles expired.");
    }
}
#endif
