using System.Collections.Generic;
using System.Linq;
using FuzzyFrenzy.Domain;
using NUnit.Framework;

public class GravityAndRefillTests
{
    [Test]
    public void Tiles_FallButStopAtBones()
    {
        var board = TestBoards.Parse("R", ".", "X", ".", "G");
        var falls = new List<FallMove>();

        GravitySolver.Apply(board, falls);

        Assert.That(TestBoards.Rows(board), Is.EqualTo(new[] { ".", "R", "X", ".", "G" }));
        Assert.That(falls.Count, Is.EqualTo(1));
    }

    [Test]
    public void Refill_EntersBelowTheBoneAboveIt()
    {
        var board = TestBoards.Parse(".", "R", "X", ".", "G");
        TestBoards.Force(board, "BY");
        var spawns = new List<SpawnedTile>();

        RefillPlanner.Fill(board, new SeededRandomSource(1), spawns);

        Assert.That(TestBoards.Rows(board), Is.EqualTo(new[] { "B", "R", "X", "Y", "G" }));
        Assert.That(spawns[0].SpawnFromY, Is.EqualTo(-1), "Top cell enters from above the board.");
        Assert.That(spawns[1].SpawnFromY, Is.EqualTo(2), "Cell under the bone enters from the bone.");
    }

    [Test]
    public void SameSeed_GeneratesTheSameBoard()
    {
        var first = TestBoards.Parse("......", "......", "......", "......", "......", "......");
        var second = TestBoards.Parse("......", "......", "......", "......", "......", "......");

        BoardGenerator.Generate(first, new SeededRandomSource(42));
        BoardGenerator.Generate(second, new SeededRandomSource(42));

        Assert.That(TestBoards.Rows(first), Is.EqualTo(TestBoards.Rows(second)));
        Assert.That(MatchFinder.HasAnyMatch(first), Is.False, "A new board never starts with a match.");
    }
}
