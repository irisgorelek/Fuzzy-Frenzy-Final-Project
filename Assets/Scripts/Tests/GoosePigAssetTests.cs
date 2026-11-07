using FuzzyFrenzy.Domain;
using NUnit.Framework;
using UnityEditor;

/// <summary>
/// Verifies the real Unity assets used by gameplay, complementing the Unity-free domain tests.
/// </summary>
public class GoosePigAssetTests
{
    private const string Animals = "Assets/ScriptableObjects/Animals/";

    [Test]
    public void GooseAsset_IsConfiguredToWakeAdjacentCyclingPigs()
    {
        Animal goose = Load(Animals + "Goose.asset");

        Assert.That(goose._id, Is.EqualTo("goose"));
        Assert.That(goose._canMatch, Is.True);
        Assert.That(goose._canSwap, Is.True);
        Assert.That(goose._affectedByGravity, Is.True);
        Assert.That(goose._traits.HasFlag(TileTraits.PermanentlyWakesAdjacent), Is.True);
        Assert.That(goose.color.a, Is.GreaterThan(0f), "A zero alpha makes the goose invisible on the board.");
    }

    [Test]
    public void CyclingPigAssets_AllPointToTheOriginalPigAsTheirPermanentForm()
    {
        Animal originalPig = Load(Animals + "Pig.asset");
        Animal awake = Load(Animals + "PigStates/CyclingPigAwake.asset");
        Animal sleep1 = Load(Animals + "PigStates/CyclingPigDeepSleep.asset");
        Animal sleep2 = Load(Animals + "PigStates/CyclingPigNearlyAwake.asset");
        var cyclingForms = new[] { awake, sleep1, sleep2 };

        Assert.That(originalPig._matchGroupId, Is.EqualTo("pig"));

        foreach (Animal form in cyclingForms)
        {
            Assert.That(form._traits.HasFlag(TileTraits.CanBePermanentlyAwakened), Is.True,
                $"{form.name} must opt into the goose wake ability.");
            Assert.That(form._permanentAwakeForm, Is.SameAs(originalPig),
                $"{form.name} must permanently become the original Pig asset.");
            Assert.That(form._matchGroupId, Is.EqualTo("pig"));
            Assert.That(form._affectedByGravity, Is.True);
        }

        Assert.That(awake._canMatch, Is.True);
        Assert.That(awake._canSwap, Is.True);
        Assert.That(sleep1._canMatch, Is.False);
        Assert.That(sleep1._canSwap, Is.False);
        Assert.That(sleep2._canMatch, Is.False);
        Assert.That(sleep2._canSwap, Is.False);

        // Validate the actual visual sequence configured in the Inspector.
        Assert.That(awake._nextTurnForm, Is.SameAs(sleep1));
        Assert.That(sleep1._nextTurnForm, Is.SameAs(sleep2));
        Assert.That(sleep2._nextTurnForm, Is.SameAs(awake));
    }

    private static Animal Load(string path)
    {
        Animal animal = AssetDatabase.LoadAssetAtPath<Animal>(path);
        Assert.That(animal, Is.Not.Null, $"Missing Animal asset at {path}");
        return animal;
    }
}
