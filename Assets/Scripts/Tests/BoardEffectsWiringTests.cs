using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class BoardEffectsWiringTests
{
    [Test]
    public void SavedLevel_InheritsDistinctParticlePrefabsAndSheepCue()
    {
        var scene = EditorSceneManager.OpenPreviewScene("Assets/Prefabs/Scenes/Level.unity");
        try
        {
            var view = scene.GetRootGameObjects().SelectMany(x => x.GetComponentsInChildren<BoardView>(true)).Single();
            var serialized = new SerializedObject(view);
            var effects = serialized.FindProperty("_boardMatchEffects").objectReferenceValue as BoardMatchEffects;
            Assert.That(effects, Is.Not.Null, "Saved scene must inherit the effects manager, not only the open editor instance.");
            Assert.That(effects.gameObject, Is.SameAs(view.gameObject));
            var manager = new SerializedObject(effects);
            Assert.That(manager.FindProperty("_view").objectReferenceValue, Is.SameAs(view));
            string[] fields = { "_match", "_boneBreak", "_blackSheep", "_bomb", "_sheepAnger" };
            string[] names = { "MatchPop", "BoneBreak", "BlackSheepLine", "BombBurstBloom", "SheepAnger" };
            for (int i = 0; i < fields.Length; i++)
            {
                var prefab = manager.FindProperty(fields[i]).objectReferenceValue as BoardParticleEffect;
                Assert.That(prefab, Is.Not.Null, fields[i]);
                Assert.That(prefab.name, Is.EqualTo(names[i]), fields[i] + " must use its own effect.");
                Assert.That(prefab.GetComponentsInChildren<ParticleSystem>(true), Is.Not.Empty);
                foreach (var graphic in prefab.GetComponentsInChildren<ParticleCanvasGraphic>(true))
                {
                    Assert.That(graphic.enabled, Is.False, "Canvas bridges are retained but must not render.");
                    Assert.That(graphic.raycastTarget, Is.False, "Particles must not intercept swipes.");
                }
                foreach (var ps in prefab.GetComponentsInChildren<ParticleSystem>(true))
                {
                    Assert.That(ps.GetComponent<ParticleSystemRenderer>().enabled, Is.True);
                    Assert.That(ps.main.scalingMode, Is.EqualTo(ParticleSystemScalingMode.Hierarchy));
                    Assert.That(ps.GetComponent<ParticleSystemRenderer>().sortingLayerName, Is.EqualTo("UI"));
                }
            }
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    [Test]
    public void BonePrefab_UsesLargeFragmentsWithoutTheNormalWoolPuff()
    {
        var bone = AssetDatabase.LoadAssetAtPath<BoardParticleEffect>("Assets/Prefabs/VFX/BoardJuice/BoneBreak.prefab");
        var systems = bone.GetComponentsInChildren<ParticleSystem>(true);
        Assert.That(systems.Select(x => x.name), Is.EquivalentTo(new[] { "BrokenBones", "FractureFlash" }));
        var fragments = systems.Single(x => x.name == "BrokenBones");
        Assert.That(fragments.main.startSize.constantMin, Is.GreaterThanOrEqualTo(0.44f));
        Assert.That(fragments.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture.name, Is.EqualTo("BoneChip"));
    }

    [Test]
    public void NormalPrefab_UsesFivePointStarsWithValidSize()
    {
        var match = AssetDatabase.LoadAssetAtPath<BoardParticleEffect>("Assets/Prefabs/VFX/BoardJuice/MatchPop.prefab");
        var stars = match.GetComponentsInChildren<ParticleSystem>().Single(x => x.name == "HoneyStars");
        Assert.That(stars.main.startSize.constantMin, Is.GreaterThan(0), "Designer tuning may change star size.");
        Assert.That(stars.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture.name, Is.EqualTo("Star"));
    }

    [Test]
    public void LinePrefab_HeadsEmitInNativePreviewAndStarsAreReadable()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<BoardParticleEffect>("Assets/Prefabs/VFX/BoardJuice/BlackSheepLine.prefab");
        var heads = prefab.GetComponentsInChildren<ParticleSystem>().Where(x => x.name.StartsWith("DirectionalStreaks")).ToArray();
        Assert.That(heads.Length, Is.EqualTo(2));
        foreach (var ps in heads)
        {
            Assert.That(ps.emission.burstCount, Is.EqualTo(1));
            Assert.That(ps.emission.GetBurst(0).count.constant, Is.EqualTo(1));
            Assert.That(ps.main.startSizeY.constant, Is.GreaterThanOrEqualTo(0.42f));
            var texture = ps.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture;
            Assert.That(texture, Is.Not.Null);
            Assert.That(ps.GetComponent<ParticleSystemRenderer>().enabled, Is.True);
        }
        var stars = prefab.GetComponentsInChildren<ParticleSystem>().Single(x => x.name == "LavenderStars");
        Assert.That(stars.main.startSize.constantMin, Is.GreaterThanOrEqualTo(0.3f));
        Assert.That(stars.main.startLifetime.constantMax, Is.GreaterThanOrEqualTo(0.85f));
    }

    [Test]
    public void AngerSequence_ThreeStaggeredMarksFinishBeforeExplosion()
    {
        var cue = AssetDatabase.LoadAssetAtPath<BoardParticleEffect>("Assets/Prefabs/VFX/BoardJuice/SheepAnger.prefab");
        var marks = cue.GetComponentsInChildren<ParticleSystem>();
        Assert.That(marks.Length, Is.EqualTo(3));
        for (int i = 0; i < marks.Length; i++)
        {
            Assert.That(marks[i].main.startDelay.constant, Is.EqualTo(i * BoardEffectsTiming.SheepAngerStep));
            Assert.That(marks[i].main.startLifetime.constant, Is.LessThan(2 * BoardEffectsTiming.SheepAngerStep));
            Assert.That(marks[i].main.startDelay.constant + marks[i].main.startLifetime.constant,
                Is.LessThanOrEqualTo(BoardEffectsTiming.SheepAnticipation));
        }
    }

    [Test]
    public void Bomb_AnimatesOneCloudAndEndsWithTileClear()
    {
        var bomb = AssetDatabase.LoadAssetAtPath<BoardParticleEffect>("Assets/Prefabs/VFX/BoardJuice/BombBurstBloom.prefab");
        Assert.That(bomb, Is.Not.Null);
        var cloud = bomb.GetComponentsInChildren<ParticleSystem>().Single(x => x.name == "AnimatedBlast");
        Assert.That(cloud.emission.GetBurst(0).count.constant, Is.EqualTo(1));
        Assert.That(cloud.main.startLifetime.constant, Is.EqualTo(BoardEffectsTiming.BombImpact));
        var sheet = cloud.textureSheetAnimation;
        Assert.That(sheet.enabled, Is.True);
        Assert.That(sheet.numTilesX * sheet.numTilesY, Is.EqualTo(24));
        Assert.That(sheet.cycleCount, Is.EqualTo(1));
        Assert.That(cloud.GetComponent<ParticleSystemRenderer>().sharedMaterial.mainTexture.name, Is.EqualTo("BombBloomAtlas"));
        foreach (var ps in bomb.GetComponentsInChildren<ParticleSystem>())
            Assert.That(ps.main.startDelay.constantMax + ps.main.startLifetime.constantMax,
                Is.LessThanOrEqualTo(BoardEffectsTiming.BombImpact + 0.001f), ps.name);
    }
}
