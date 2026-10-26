using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

public class BoardTileDropTests
{
    [TestCase(true)]
    [TestCase(false)]
    public void SheepBlast_ReportsRefillSpawnsAndExistingFalls(bool horizontal)
    {
        var animal = ScriptableObject.CreateInstance<Animal>();
        var config = ScriptableObject.CreateInstance<BoardConfig>();
        try
        {
            animal._id = "test";
            animal._affectedByGravity = true;
            config.weidth = config.height = 3;
            config.animals = new List<Animal> { animal };
            var board = new Board(config);
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++) board.SetAnimalInCell(new Vector2Int(x,y), animal);
            var falls = new List<Board.FallMove>();
            var spawns = new List<Board.SpawnInfo>();
            board.TriggerSheepSwipeBlast(Vector2Int.one, horizontal, falls, spawns);
            Assert.AreEqual(3, spawns.Count);
            Assert.AreEqual(horizontal ? 3 : 0, falls.Count);
            foreach (var spawn in spawns)
            {
                Assert.AreEqual(-1, spawn.spawnFromY);
                Assert.AreSame(spawn.animal, board.GetAnimalFromCell(spawn.cell));
            }
        }
        finally { Object.DestroyImmediate(animal); Object.DestroyImmediate(config); }
    }

    [Test]
    public void Drop_ReachesGroundThenReboundsAndSettlesExactly()
    {
        var go = new GameObject("Drop test", typeof(RectTransform));
        object tween = null;
        try
        {
            var rect = go.GetComponent<RectTransform>();
            rect.position = new Vector3(0,100,0);
            var method = typeof(BoardTileDropAnimation).GetMethod("Create");
            tween = method.Invoke(null, new object[] { rect, Vector3.zero, Vector3.up * 8, .2f, .14f });
            var extensions = tween.GetType().Assembly.GetType("DG.Tweening.TweenExtensions");
            var goTo = extensions.GetMethods().Single(m => m.Name == "Goto" && !m.IsGenericMethod && m.GetParameters().Length == 3);
            goTo.Invoke(null, new[] { tween, (object).2f, false });
            Assert.Less(rect.position.magnitude, .001f);
            goTo.Invoke(null, new[] { tween, (object).263f, false });
            Assert.That(rect.position.y, Is.EqualTo(8).Within(.001f));
            goTo.Invoke(null, new[] { tween, (object).34f, false });
            Assert.Less(rect.position.magnitude, .001f);
        }
        finally
        {
            if (tween != null)
                tween.GetType().Assembly.GetType("DG.Tweening.TweenExtensions").GetMethods()
                    .Single(m => m.Name == "Kill" && !m.IsGenericMethod && m.GetParameters().Length == 2)
                    .Invoke(null, new[] { tween, (object)false });
            Object.DestroyImmediate(go);
        }
    }
}
