using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class BoardBoneFeedbackTests
{
    [Test]
    public void SheepWave_BreaksBoneBeforeRefillAndDoesNotReportItTwice()
    {
        var animal = ScriptableObject.CreateInstance<Animal>();
        var bone = ScriptableObject.CreateInstance<Animal>();
        var config = ScriptableObject.CreateInstance<BoardConfig>();
        try
        {
            animal._id = "animal";
            animal._affectedByGravity = true;
            bone._id = "bone";
            bone._affectedByGravity = false;
            config.weidth = config.height = 3;
            config.animals = new List<Animal> { animal };
            config.boneBlock = bone;
            var board = new Board(config);
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++) board.SetAnimalInCell(new Vector2Int(x,y), animal);
            board.SetAnimalInCell(Vector2Int.one, bone);
            int breaks = 0;
            board.OnBoneBlockBroken += _ => breaks++;
            board.BreakBonesAdjacentTo(new List<Vector2Int> { new Vector2Int(0,1) });
            Assert.AreEqual(1, breaks);
            Assert.IsNull(board.GetAnimalFromCell(Vector2Int.one), "Bone breaks during the wave, without waiting for refill.");
            Assert.AreSame(animal, board.GetAnimalFromCell(new Vector2Int(1,0)));
            board.TriggerSheepSwipeBlast(new Vector2Int(0,1), true);
            Assert.AreEqual(1, breaks, "Final model clear must not duplicate the crack or reward.");
            Assert.IsNotNull(board.GetAnimalFromCell(Vector2Int.one));
        }
        finally
        {
            Object.DestroyImmediate(animal);
            Object.DestroyImmediate(bone);
            Object.DestroyImmediate(config);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ClearCells_BoneAdjacentToTwoClears_ReportsItsPositionOnceBeforeRemoval(bool configuredInLevel)
    {
        var a = ScriptableObject.CreateInstance<Animal>();
        var b = ScriptableObject.CreateInstance<Animal>();
        var bone = ScriptableObject.CreateInstance<Animal>();
        var config = ScriptableObject.CreateInstance<BoardConfig>();
        try
        {
            a._id = "a";
            b._id = "b";
            bone._id = "bone";
            config.weidth = config.height = 3;
            config.animals = new List<Animal> {a, b};
            config.boneBlock = configuredInLevel ? bone : null;
            var board = new Board(config);
            if (!configuredInLevel) board.DebugSetSpecialAnimals(null, bone);
            for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++) board.SetAnimalInCell(new Vector2Int(x, y), (x + y) % 2 == 0 ? a : b);
            var center = new Vector2Int(1, 1);
            board.SetAnimalInCell(center, bone);
            var positions = new List<Vector2Int>();
            board.OnBoneBlockBroken += position =>
            {
                positions.Add(position);
                Assert.That(board.GetAnimalFromCell(position), Is.SameAs(bone), "Feedback must fire before removal/gravity.");
            };
            board.ClearCells(new[] {new Vector2Int(0, 1), new Vector2Int(1, 0)});
            Assert.That(positions, Is.EqualTo(new[] {center}));
        }
        finally
        {
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(bone);
        }
    }
}
