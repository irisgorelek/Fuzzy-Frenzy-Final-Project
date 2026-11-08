using System.Linq;
using FuzzyFrenzy.Domain;
using NUnit.Framework;

/// <summary>Bones, the black sheep blast and shuffling, tested on the core. Boards are written top row first.</summary>
public class BoardRulesTests
{
    [Test]
    public void BlackSheepWave_BreaksANearbyBoneOnce()
    {
        var board = TestBoards.Parse(
            "RGB",
            "XKR",
            "BRG");
        var game = TestBoards.Game(board);

        var result = game.TrySwap(new GridPos(1, 1), new GridPos(1, 0), TestBoards.LastMove);

        var blast = result.Steps.OfType<LineBlastStep>().Single();
        int brokenInWaves = blast.Waves.Sum(w => w.TilesBroken.Count);
        int brokenAfter = result.Steps.OfType<TilesBrokenStep>().Sum(s => s.Cells.Count);
        Assert.That(brokenInWaves, Is.EqualTo(1), "The bone breaks as the wave passes it.");
        Assert.That(brokenAfter, Is.EqualTo(0), "The final line clear must not break it again.");
        Assert.That(game.Session.CollectedCount("bone"), Is.EqualTo(1));
    }

    [Test]
    public void BoneNextToTwoClearedCells_BreaksOnce()
    {
        var board = TestBoards.Parse(
            "RGR",
            "GXG",
            "RGR");
        var game = TestBoards.Game(board);

        var result = game.ClearCells(new[] { new GridPos(0, 1), new GridPos(1, 0) }, new TurnContext(0, 10, false));

        var broken = result.Steps.OfType<TilesBrokenStep>().First();
        Assert.That(broken.Cells, Is.EqualTo(new[] { new GridPos(1, 1) }));
        Assert.That(game.Session.CollectedCount("bone"), Is.EqualTo(1));
    }

    // K at (1,0) swiped down blasts row 1, so row 0 falls into it. Swiped right, it blasts column 2: nothing falls.
    [TestCase(1, 1, 3)]
    [TestCase(2, 0, 0)]
    public void BlackSheepBlast_ReportsFallsAndSpawnsFromAboveTheBoard(int toX, int toY, int expectedFalls)
    {
        var board = TestBoards.Parse(
            "RKB",
            "YGR",
            "BRG");

        var result = TestBoards.Game(board).TrySwap(new GridPos(1, 0), new GridPos(toX, toY), TestBoards.LastMove);

        var gravity = result.Steps.OfType<GravityStep>().First();
        Assert.That(gravity.Falls.Count, Is.EqualTo(expectedFalls));
        Assert.That(gravity.Spawns.Count, Is.EqualTo(3));
        Assert.That(gravity.Spawns.All(s => s.SpawnFromY == -1), Is.True);
        foreach (var spawn in gravity.Spawns)
            Assert.That(gravity.After.TypeAt(spawn.Cell), Is.EqualTo(spawn.Type));
    }

    [Test]
    public void Shuffle_LeavesBlockersInPlace()
    {
        var board = TestBoards.Parse(
            "RGBY",
            "GXYR",
            "BYRG",
            "YRGB");

        BoardOperations.ShuffleSwappable(board, new SeededRandomSource(3));

        Assert.That(board.TypeAt(new GridPos(1, 1)), Is.EqualTo(TestBoards.TypeOf('X')));
    }
}
