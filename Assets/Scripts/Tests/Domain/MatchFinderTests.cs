using System.Collections.Generic;
using System.Linq;
using FuzzyFrenzy.Domain;
using NUnit.Framework;

/// <summary>Tests for the Unity-free core. Boards are written as strings, top row first (see TestBoards).</summary>
public class MatchFinderTests
{
    [Test]
    public void CrossingRuns_AreFoundOnceEach()
    {
        var board = TestBoards.Parse(
            "RRRG",
            "GBRB",
            "BGRY");

        var matches = new List<GridPos>();
        MatchFinder.Find(board, matches);

        Assert.That(matches.Count, Is.EqualTo(5));
        Assert.That(matches, Is.Unique);
    }

    [Test]
    public void Bones_BreakARun()
    {
        var board = TestBoards.Parse("RRXRR");
        Assert.That(MatchFinder.HasAnyMatch(board), Is.False);
    }
}
