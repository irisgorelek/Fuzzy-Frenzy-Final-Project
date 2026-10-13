#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Manual editor diagnostic: enters through a real menu button, then exercises gameplay.</summary>
public sealed class BoardEffectsGameplayCheck : MonoBehaviour
{
    private const string Output = ".utmp/BoardEffectsPreview";
    private readonly List<string> _passed = new();
    public bool CaptureScreenshots;
    private static readonly BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    private IEnumerator Start()
    {
        DontDestroyOnLoad(gameObject);
        Directory.CreateDirectory(Output);
        File.WriteAllText(Output + "/GameplayCheck.txt", "RUNNING");
        var selector = FindFirstObjectByType<LevelSelectUI>(FindObjectsInactive.Include);
        if (selector == null) throw new InvalidOperationException("Start this check from MainMenu+Shop in Play mode.");
        selector.gameObject.SetActive(true);
        yield return null;
        yield return null;
        var candidates = selector.GetComponentsInChildren<LevelButtonUI>(true)
            .Where(x => x.GetComponent<Button>() != null && x.GetComponent<Button>().interactable).ToArray();
        if (candidates.Length == 0) throw new InvalidOperationException("No unlocked level button.");
        candidates[candidates.Length - 1].GetComponent<Button>().onClick.Invoke();
        float deadline = Time.realtimeSinceStartup + 15;
        BoardController controller = null;
        while (Time.realtimeSinceStartup < deadline)
        {
            controller = FindFirstObjectByType<BoardController>();
            if (SceneManager.GetActiveScene().name == "Level" && controller != null && controller.CurrentBoard != null) break;
            yield return null;
        }
        if (controller == null || controller.CurrentBoard == null) throw new InvalidOperationException("Menu selection did not enter Level.");
        yield return Settle(controller);
        var view = FindFirstObjectByType<BoardView>();
        var original = controller.Config;
        var config = Instantiate(original);
        config.goalType = PointsOrMatches.matches;
        config.goal = 100000;
        config.collectGoals = new List<AnimalGoal>();
        config.maxMoves = 100;
        config.blackSheepRollChance = 0;
        config.animals = original.animals.Where(a => a != null && a._canSwap && a._canMatch &&
            a != original.wolf && a != original.sheep && a != original.blackSheep).ToList();
        if (config.animals.Count < 3) throw new InvalidOperationException("Diagnostic needs three normal animal types.");
        typeof(BoardController).GetField("_cfg", Private).SetValue(controller, config);
        // Exercise Inspector actions on a level that does not normally define either special tile.
        var configuredSheep = config.blackSheep;
        var configuredBone = config.boneBlock;
        config.blackSheep = null;
        config.boneBlock = null;
        controller.InitializeGame();
        yield return null;
        yield return null;
        var cheats = FindFirstObjectByType<LevelDebugFlowCheats>();
        if (cheats == null) throw new InvalidOperationException("Debug cheat component is missing.");
        cheats.AddBlackSheepToCenter();
        VerifyDebugCenter(controller, view, cheats, "_blackSheep");
        var isBlackSheep = typeof(BoardController).GetMethod("IsAnySheep", Private);
        var debugCenter = new Vector2Int(config.weidth / 2, config.height / 2);
        if (!(bool)isBlackSheep.Invoke(controller, new object[] {controller.CurrentBoard.GetAnimalFromCell(debugCenter)}))
            throw new InvalidOperationException("Placed debug sheep is not recognized by gameplay.");
        cheats.AddBoneToCenter();
        VerifyDebugCenter(controller, view, cheats, "_boneBlock");
        if (config.blackSheep != null || config.boneBlock != null)
            throw new InvalidOperationException("Debug placement changed the level configuration.");
        _passed.Add("Debug buttons: black sheep and bone placed visibly on a level with neither configured.");
        config.blackSheep = configuredSheep;
        config.boneBlock = configuredBone;
        controller.InitializeGame();
        yield return null;
        yield return null;
        var board = controller.CurrentBoard;
        Fill(board, view, config);
        var a = config.animals[0];
        var b = config.animals[1];
        board.SetAnimalInCell(new Vector2Int(0, 0), a);
        board.SetAnimalInCell(new Vector2Int(1, 0), a);
        board.SetAnimalInCell(new Vector2Int(2, 0), b);
        board.SetAnimalInCell(new Vector2Int(2, 1), a);
        board.SetAnimalInCell(new Vector2Int(1, 1), config.boneBlock);
        view.AssignSprites(board);
        controller.OnSwapRequested(new Vector2Int(2, 1), new Vector2Int(2, 0));
        yield return Observe("NormalAndBone", 2f, "MatchPop", "BoneBreak");
        yield return Settle(controller);
        Fill(board, view, config);
        board.SetAnimalInCell(new Vector2Int(2, 2), config.blackSheep);
        view.AssignSprites(board);
        controller.OnSwapRequested(new Vector2Int(2, 2), new Vector2Int(2, 3));
        yield return Observe("SheepHorizontal", 3f, "BlackSheepLine", "SheepAnger");
        yield return Settle(controller);
        Fill(board, view, config);
        board.SetAnimalInCell(new Vector2Int(2, 2), config.blackSheep);
        view.AssignSprites(board);
        controller.OnSwapRequested(new Vector2Int(2, 2), new Vector2Int(3, 2));
        yield return Observe("SheepVertical", 3f, "BlackSheepLine", "SheepAnger");
        yield return Settle(controller);
        Fill(board, view, config);
        var bomb = FindFirstObjectByType<BombPowerUp>();
        var economy = GameBootstrapper.Instance.Economy;
        int stock = economy.GetBoosterCount(BoosterEffectType.FuzzyBlast);
        if (stock == 0) economy.AddBooster(BoosterEffectType.FuzzyBlast, 1);
        try
        {
            bomb.TryUseBomb(new Vector2Int(2, 2));
            yield return Observe("BombGameplay", 3f, "BombBurstBloom");
            yield return Settle(controller);
        }
        finally
        {
            int remaining = economy.GetBoosterCount(BoosterEffectType.FuzzyBlast);
            if (remaining < stock) economy.AddBooster(BoosterEffectType.FuzzyBlast, stock - remaining);
            typeof(BoardController).GetField("_cfg", Private).SetValue(controller, original);
            Destroy(config);
        }
        yield return new WaitForSeconds(1.2f);
        if (FindObjectsByType<BoardParticleEffect>(FindObjectsSortMode.None).Length != 0)
            throw new InvalidOperationException("Particles did not return to their pools.");
        File.WriteAllText(Output + "/GameplayCheck.txt", "PASS\n" + string.Join("\n", _passed) + "\nParticle cleanup passed.");
        Debug.Log("Board effects GAMEPLAY check PASSED: real normal swap + bones, both black-sheep directions, bomb booster.");
        Destroy(gameObject);
    }

    private static void Fill(Board board, BoardView view, BoardConfig config)
    {
        for (int y = 0; y < config.height; y++)
        for (int x = 0; x < config.weidth; x++)
            board.SetAnimalInCell(new Vector2Int(x, y), config.animals[(x + y * 2) % 3]);
        view.AssignSprites(board);
    }

    private static void VerifyDebugCenter(BoardController controller, BoardView view, LevelDebugFlowCheats cheats, string field)
    {
        var expected = (Animal)typeof(LevelDebugFlowCheats).GetField(field, Private).GetValue(cheats);
        var board = controller.CurrentBoard;
        var center = new Vector2Int(board.Width / 2, board.Height / 2);
        var cells = (Dictionary<Vector2Int, CellView>)typeof(BoardView).GetField("_cells", Private).GetValue(view);
        if (expected == null || board.GetAnimalFromCell(center) != expected || cells[center].CellImage.sprite != expected._sprite)
            throw new InvalidOperationException(field + " debug button did not update the centre tile and its sprite.");
    }

    private IEnumerator Observe(string label, float duration, params string[] expected)
    {
        var seen = new HashSet<string>();
        bool captured = false;
        bool nativeCaptured = false;
        float nativeCaptureAt = float.PositiveInfinity;
        int maxBombInstances = 0;
        int maxAngerInstances = 0;
        float firstAnger = float.PositiveInfinity;
        float firstLine = float.PositiveInfinity;
        var angerStarts = new Dictionary<string, float>();
        int angerCaptures = 0;
        Vector3? angerPosition = null;
        float angerTileWidth = 0;
        bool heardBoneBreak = false;
        bool sawLineHeads = false;
        float end = Time.realtimeSinceStartup + duration;
        while (Time.realtimeSinceStartup < end)
        {
            var instances = FindObjectsByType<BoardParticleEffect>(FindObjectsSortMode.None);
            var audio = AudioManager.instance;
            if (audio != null)
            {
                var source = (AudioSource)typeof(AudioManager).GetField("_boneBreak", Private).GetValue(audio);
                if (source == null || source.clip == null || source.clip.name != "BoneImpactCrack")
                    throw new InvalidOperationException("Bone break audio is not wired on the live AudioManager.");
                heardBoneBreak |= source.isPlaying;
            }
            maxBombInstances = Mathf.Max(maxBombInstances, instances.Count(x => x.name.StartsWith("BombBurst")));
            var anger = instances.Where(x => x.name.StartsWith("SheepAnger")).ToArray();
            maxAngerInstances = Mathf.Max(maxAngerInstances, anger.Length);
            if (anger.Any(x => x.LiveParticleCount > 0)) firstAnger = Mathf.Min(firstAnger, Time.realtimeSinceStartup);
            if (anger.Length > 0)
            {
                var marks = anger[0].GetComponentsInChildren<ParticleSystem>();
                foreach (var mark in marks.Where(x => x.particleCount > 0))
                    if (!angerStarts.ContainsKey(mark.name)) angerStarts.Add(mark.name, Time.realtimeSinceStartup);
                if (marks.Any(x => x.name == "AngerMark1" && x.particleCount > 0) &&
                    marks.Any(x => x.name == "AngerMark3" && x.particleCount > 0))
                    throw new InvalidOperationException("First anger mark must finish before the third starts.");
                if (angerCaptures < 3 && Time.realtimeSinceStartup >= firstAnger + 0.12f + angerCaptures * BoardEffectsTiming.SheepAngerStep)
                {
                    CaptureNative(label + "_Anger" + (angerCaptures + 1));
                    angerCaptures++;
                }
                angerPosition = anger[0].transform.position;
                angerTileWidth = anger[0].transform.lossyScale.x;
                var cueRenderer = anger[0].GetComponentInChildren<ParticleSystemRenderer>();
                foreach (var other in instances.Where(x => x != anger[0]).SelectMany(x => x.GetComponentsInChildren<ParticleSystemRenderer>()))
                    if (other.sortingLayerID == cueRenderer.sortingLayerID && other.sortingOrder >= cueRenderer.sortingOrder)
                        throw new InvalidOperationException("Anger cue must sort above subsequent line effects.");
            }
            if (angerPosition.HasValue)
            {
                var lineOrigin = instances.FirstOrDefault(x => x.name.StartsWith("BlackSheepLine") &&
                    x.GetComponentsInChildren<ParticleSystem>().Any(ps => ps.name.StartsWith("DirectionalStreaks") && ps.particleCount > 0));
                if (lineOrigin != null && Vector3.Distance(angerPosition.Value, lineOrigin.transform.position) > angerTileWidth)
                    throw new InvalidOperationException("Anger cue was displaced more than a tile from the sheep.");
            }
            if (instances.Any(x => x.name.StartsWith("BlackSheepLine") && x.LiveParticleCount > 0))
                firstLine = Mathf.Min(firstLine, Time.realtimeSinceStartup);
            sawLineHeads |= instances.SelectMany(x => x.GetComponentsInChildren<ParticleSystem>())
                .Any(x => x.name.StartsWith("DirectionalStreaks") && x.particleCount > 0);
            foreach (var fx in FindObjectsByType<BoardParticleEffect>(FindObjectsSortMode.None))
            {
                if (fx.GetComponentsInChildren<ParticleCanvasGraphic>().Any(x => x.enabled))
                    throw new InvalidOperationException("Canvas particle bridge must stay disabled.");
                if (fx.GetComponentsInChildren<ParticleSystemRenderer>().Any(x => !x.enabled))
                    throw new InvalidOperationException("Native particle renderer must stay enabled.");
                if (fx.LiveParticleCount > 0)
                    foreach (string name in expected) if (fx.name.StartsWith(name)) seen.Add(name);
            }
            if (seen.Count == expected.Length && float.IsPositiveInfinity(nativeCaptureAt))
                nativeCaptureAt = Time.realtimeSinceStartup + 0.1f;
            if (!nativeCaptured && Time.realtimeSinceStartup >= nativeCaptureAt)
            {
                CaptureNative(label);
                nativeCaptured = true;
            }
            if (CaptureScreenshots && !captured && seen.Count == expected.Length)
            {
                // Keep short-lived particles visible if the editor delays its next rendered frame.
                yield return new WaitForSeconds(0.06f);
                var systems = FindObjectsByType<BoardParticleEffect>(FindObjectsSortMode.None)
                    .SelectMany(x => x.GetComponentsInChildren<ParticleSystem>()).ToArray();
                foreach (var ps in systems) ps.Pause(false);
                yield return new WaitForEndOfFrame();
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Output + "/" + label + ".png", texture.EncodeToPNG());
                Destroy(texture);
                foreach (var ps in systems) if (ps != null) ps.Play(false);
                captured = true;
            }
            yield return null;
        }
        if (seen.Count != expected.Length)
        {
            string failure = label + " missing: " + string.Join(", ", expected.Where(x => !seen.Contains(x)));
            File.WriteAllText(Output + "/GameplayCheck.txt", "FAIL: " + failure);
            throw new InvalidOperationException(failure);
        }
        if (label == "NormalAndBone" && AudioManager.instance != null && AudioManager.instance.SfxOn && !heardBoneBreak)
            throw new InvalidOperationException("Bone break particles appeared without the crack sound playing.");
        if (label.StartsWith("Sheep") && !sawLineHeads)
            throw new InvalidOperationException(label + " did not emit directional streaks.");
        if (label.StartsWith("Sheep") && (maxAngerInstances != 1 || firstAnger >= firstLine))
            throw new InvalidOperationException(label + " must show exactly one anger cue before the line launches.");
        if (label.StartsWith("Sheep"))
        {
            if (!angerStarts.TryGetValue("AngerMark1", out float first) ||
                !angerStarts.TryGetValue("AngerMark2", out float second) ||
                !angerStarts.TryGetValue("AngerMark3", out float third) || !(first < second && second < third))
                throw new InvalidOperationException("Three anger marks must appear in order.");
            if (firstLine - firstAnger < BoardEffectsTiming.SheepAnticipation - 0.1f)
                throw new InvalidOperationException("Line explosion started before the anger anticipation finished.");
        }
        if (!label.StartsWith("Sheep") && maxAngerInstances != 0)
            throw new InvalidOperationException(label + " unexpectedly spawned a sheep anger cue.");
        if (label == "BombGameplay" && maxBombInstances != 1)
            throw new InvalidOperationException("Bomb must use one central instance; observed " + maxBombInstances);
        _passed.Add(label + ": " + string.Join(", ", seen));
    }

    // Render the real Canvas camera directly; no end-of-frame wait or UI particle bridge.
    private static void CaptureNative(string label)
    {
        var canvas = FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .First(x => x.isRootCanvas && x.worldCamera != null);
        var camera = canvas.worldCamera;
        var previousTarget = camera.targetTexture;
        var previousActive = RenderTexture.active;
        var target = new RenderTexture(Screen.width, Screen.height, 24);
        Texture2D image = null;
        try
        {
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            image = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Output + "/" + label + "_Native.png", image.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Destroy(target);
            if (image != null) Destroy(image);
        }
    }

    private static IEnumerator Settle(BoardController controller)
    {
        float end = Time.realtimeSinceStartup + 12;
        while (Time.realtimeSinceStartup < end)
        {
            var presenter = FindFirstObjectByType<SpeechBubblePresenter>();
            var tutorial = presenter == null ? null : typeof(SpeechBubblePresenter).GetField("_tutorial", Private).GetValue(presenter);
            var button = tutorial == null ? null : tutorial.GetType().GetField("continueButton").GetValue(tutorial) as Button;
            if (button != null && button.gameObject.activeInHierarchy && button.interactable) button.onClick.Invoke();
            var dialogue = FindFirstObjectByType<AnimalDialogueController>();
            bool tutorialActive = dialogue != null && (bool)typeof(AnimalDialogueController).GetField("_tutorialActive", Private).GetValue(dialogue);
            bool busy = (bool)typeof(BoardController).GetField("_isBusy", Private).GetValue(controller);
            if (!busy && !controller.IsSpeechBubbleInputBlocked && !tutorialActive) break;
            yield return null;
        }
        FindFirstObjectByType<SpeechBubblePresenter>()?.HideNormalImmediate();
        yield return new WaitForSeconds(0.7f);
    }

    private void OnDestroy()
    {
        string path = Output + "/GameplayCheck.txt";
        if (File.Exists(path) && File.ReadAllText(path) == "RUNNING")
            File.WriteAllText(path, "INTERRUPTED: check stopped before completion.");
    }
}
#endif
