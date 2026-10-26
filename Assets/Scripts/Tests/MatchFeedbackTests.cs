using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class MatchFeedbackTests
{
    [Test]
    public void BlackSheep_ClearHidesShrunkTileUntilBoardRedraw()
    {
        var root = new GameObject("Line clear test", typeof(RectTransform));
        var view = root.AddComponent<BoardView>();
        var cellGo = new GameObject("Cell", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        cellGo.transform.SetParent(root.transform, false);
        var cell = cellGo.AddComponent<CellView>();
        var image = cellGo.GetComponent<UnityEngine.UI.Image>();
        var settings = new SerializedObject(cell);
        settings.FindProperty("_image").objectReferenceValue = image;
        settings.ApplyModifiedPropertiesWithoutUndo();
        try
        {
            var cells = (System.Collections.IDictionary)typeof(BoardView).GetField("_cells", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(view);
            cells.Add(Vector2Int.zero, cell);
            var task = view.AnimateMatchPopFx(new System.Collections.Generic.List<Vector2Int> { Vector2Int.zero }, .14f, BoardClearEffect.BlackSheep);
            var tweenAssembly = typeof(BoardView).GetMethod("BuildMatchLikePop", BindingFlags.Instance | BindingFlags.NonPublic).ReturnType.Assembly;
            tweenAssembly.GetType("DG.Tweening.DOTween").GetMethod("Complete", new[] { typeof(object), typeof(bool) }).Invoke(null, new object[] { view, true });
            Assert.IsTrue(task.IsCompletedSuccessfully);
            Assert.IsFalse(image.enabled, "Finished tiles must not reappear while later waves and particles play.");
            Assert.AreEqual(Vector3.one, image.rectTransform.localScale);
        }
        finally { Object.DestroyImmediate(root); }
    }

    [Test]
    public void NormalStars_UseThePuffsUpwardArcWithoutExtraLaunchSpeed()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/BoardJuice/MatchPop.prefab");
        var systems = prefab.GetComponentsInChildren<ParticleSystem>(true);
        var puffs = systems.Single(p => p.name == "WarmPuffs");
        var stars = systems.Single(p => p.name == "HoneyStars");
        Assert.AreEqual(ParticleSystemCurveMode.Constant, stars.main.startSpeed.mode);
        Assert.AreEqual(0, stars.main.startSpeed.constant);
        Assert.AreEqual(puffs.velocityOverLifetime.space, stars.velocityOverLifetime.space);
        foreach (float t in new[] { 0f, .5f, 1f })
        {
            Assert.AreEqual(puffs.velocityOverLifetime.y.curveMin.Evaluate(t), stars.velocityOverLifetime.y.curveMin.Evaluate(t));
            Assert.AreEqual(puffs.velocityOverLifetime.y.curveMax.Evaluate(t), stars.velocityOverLifetime.y.curveMax.Evaluate(t));
        }
        Assert.Greater(stars.velocityOverLifetime.y.curveMin.Evaluate(0), 0);
        Assert.Less(stars.velocityOverLifetime.y.curveMax.Evaluate(1), 0);
    }

    [Test]
    public void AnimalArrival_ShineFadesBehindIconAndScalesWithIt()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GoalRowView>("Assets/Prefabs/GoalRow+Moves/Animal Goal.prefab");
        var canvas = new GameObject("Arrival feedback test", typeof(RectTransform), typeof(Canvas));
        var row = Object.Instantiate(prefab, canvas.transform);
        row.gameObject.SetActive(true);
        try
        {
            var authoredShine = row.GetComponentsInChildren<UnityEngine.UI.Image>(true).Single(i => i.name == "Collection Shine");
            authoredShine.rectTransform.sizeDelta = new Vector2(165, 119);
            row.PulseIcon();
            var number = row.GetComponentInChildren<TextMeshProUGUI>(true);
            var shine = row.GetComponentsInChildren<UnityEngine.UI.Image>(true).Single(i => i.name == "Collection Shine");
            var tween = typeof(GoalRowView).GetField("_iconTween", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(row);
            var goTo = tween.GetType().Assembly.GetType("DG.Tweening.TweenExtensions").GetMethods()
                .Single(m => m.Name == "Goto" && !m.IsGenericMethod && m.GetParameters().Length == 3);
            goTo.Invoke(null, new[] { tween, (object).18f, false });
            Assert.Greater(shine.color.a, .5f);
            Assert.AreEqual(new Vector2(165, 119), shine.rectTransform.sizeDelta);
            Assert.AreEqual(row.IconRect.localScale, shine.rectTransform.localScale);
            Assert.Less(shine.transform.GetSiblingIndex(), row.IconRect.GetSiblingIndex());
            Assert.Greater(number.transform.GetSiblingIndex(), row.IconRect.GetSiblingIndex());
            Assert.AreEqual(row.IconRect.localScale, number.rectTransform.localScale);
            Assert.Greater(row.IconRect.localScale.x, 1.16f);
            goTo.Invoke(null, new[] { tween, (object).41f, false });
            Assert.Less(shine.color.a, .1f);
            row.gameObject.SetActive(false);
            // EditMode does not dispatch ordinary MonoBehaviour lifecycle callbacks.
            typeof(GoalRowView).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(row, null);
            Assert.IsFalse(shine.gameObject.activeSelf);
        }
        finally { Object.DestroyImmediate(canvas); }
    }

    [Test]
    public void SavedScene_WiresActiveClearAnimation()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Prefabs/Scenes/Level.unity");
        try
        {
            var roots = scene.GetRootGameObjects();
            var view = roots.SelectMany(r => r.GetComponentsInChildren<BoardView>(true)).Single();
            var animation = new SerializedObject(view).FindProperty("_clearAnimation").objectReferenceValue as BoardClearAnimation;
            Assert.IsNotNull(animation);
            Assert.AreSame(view, new SerializedObject(animation).FindProperty("_view").objectReferenceValue);
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void AnimalTint_DoesNotAccumulateWhenParticleInstanceIsReused()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/VFX/BoardJuice/MatchPop.prefab");
        var instance = Object.Instantiate(prefab);
        try
        {
            var tint = instance.GetComponent<AnimalMatchParticleTint>();
            var systems = instance.GetComponentsInChildren<ParticleSystem>();
            var puffs = systems.Single(p => p.name == "WarmPuffs");
            var stars = systems.Single(p => p.name == "HoneyStars");
            Color puffBase = puffs.main.startColor.colorMax, starBase = stars.main.startColor.colorMax;
            var pink = new Color(.94f, .67f, .64f);
            tint.Apply(pink);
            Color first = puffs.main.startColor.colorMax;
            float puffAmount = Vector3.Distance((Vector4)first, (Vector4)puffBase) /
                Vector3.Distance((Vector4)pink, (Vector4)puffBase);
            float starAmount = Vector3.Distance((Vector4)stars.main.startColor.colorMax, (Vector4)starBase) /
                Vector3.Distance((Vector4)pink, (Vector4)starBase);
            Assert.Greater(puffAmount, starAmount + .25f);
            Assert.AreEqual(puffBase.a, first.a);
            tint.Apply(new Color(.44f, .50f, .65f));
            tint.Apply(pink);
            Assert.AreEqual(first, puffs.main.startColor.colorMax);
        }
        finally { Object.DestroyImmediate(instance); }
    }

    [Test]
    public void GoldNumberFeedback_PreservesTextAndLeavesLabelUntinted()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GoalRowView>("Assets/Prefabs/GoalRow+Moves/Primary Goal.prefab");
        var canvas = new GameObject("Feedback test canvas", typeof(RectTransform), typeof(Canvas));
        var row = Object.Instantiate(prefab, canvas.transform);
        row.gameObject.SetActive(true);
        try
        {
            var text = new SerializedObject(row).FindProperty("_text").objectReferenceValue as TextMeshProUGUI;
            text.rectTransform.sizeDelta = new Vector2(600, 200);
            text.fontSize = 36;
            row.Set(null, "Matches: \n27", Color.white);
            text.ForceMeshUpdate();
            var label = text.textInfo.characterInfo.First(c => c.isVisible && c.character == 'M');
            var before = text.textInfo.meshInfo[label.materialReferenceIndex].colors32[label.vertexIndex];
            var pulse = typeof(GoalRowView).GetMethod("SetNumberPulse", BindingFlags.Instance | BindingFlags.NonPublic);
            pulse.Invoke(row, new object[] { 1f });
            Assert.AreEqual("Matches: \n27", text.text);
            label = text.textInfo.characterInfo.First(c => c.isVisible && c.character == 'M');
            var digit = text.textInfo.characterInfo.First(c => c.isVisible && char.IsDigit(c.character));
            Assert.AreEqual(before, text.textInfo.meshInfo[label.materialReferenceIndex].colors32[label.vertexIndex]);
            var tint = (Color32)new SerializedObject(row).FindProperty("_feedbackGold").colorValue;
            Assert.AreEqual(tint.b, text.textInfo.meshInfo[digit.materialReferenceIndex].colors32[digit.vertexIndex].b);
            pulse.Invoke(row, new object[] { 0f });
            Assert.AreEqual(before, text.textInfo.meshInfo[digit.materialReferenceIndex].colors32[digit.vertexIndex]);
        }
        finally { Object.DestroyImmediate(canvas); }
    }

    [Test]
    public void CompletedAnimalGoal_IsNotAFlightTarget()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GoalRowView>("Assets/Prefabs/GoalRow+Moves/Animal Goal.prefab");
        var row = Object.Instantiate(prefab);
        try
        {
            var pig = AssetDatabase.LoadAssetAtPath<Animal>("Assets/ScriptableObjects/Animals/Pig.asset");
            row.Set(pig._sprite, "1", Color.white);
            Assert.IsTrue(row.CanCollect);
            row.Set(pig._sprite, "0", Color.white, true);
            Assert.IsFalse(row.CanCollect);
            Assert.AreSame(pig._sprite, row.IconSprite);
        }
        finally { Object.DestroyImmediate(row.gameObject); }
    }
}
