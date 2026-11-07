using System;
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

public class Match3GameTests
{
    [Test]
    public void SwapWithoutMatch_SwapsBackAndUsesNoMove()
    {
        var rows = new[] { "RGB", "GBR", "BRG" };
        var board = TestBoards.Parse(rows);

        var result = TestBoards.Game(board).TrySwap(new GridPos(0, 0), new GridPos(1, 0), TestBoards.LastMove);

        Assert.That(result.Outcome, Is.EqualTo(TurnOutcome.NoMatch));
        Assert.That(result.Steps.Select(s => s.GetType()), Is.EqualTo(new[] { typeof(SwapStep), typeof(SwapBackStep) }));
        Assert.That(TestBoards.Rows(board), Is.EqualTo(rows));
    }

    [TestCase(1, 0, 3, 0, RejectReason.NotAdjacent)]
    [TestCase(0, 0, 1, 0, RejectReason.NotSwappable)]
    [TestCase(2, 1, 3, 1, RejectReason.EmptyCell)]
    public void IllegalSwaps_AreRejectedWithoutChangingTheBoard(int fx, int fy, int tx, int ty, RejectReason expected)
    {
        var rows = new[] { "XRGB", "GBR.", "BRG." };
        var board = TestBoards.Parse(rows);

        var result = TestBoards.Game(board).TrySwap(new GridPos(fx, fy), new GridPos(tx, ty), TestBoards.LastMove);

        Assert.That(result.Outcome, Is.EqualTo(TurnOutcome.Rejected));
        Assert.That(result.RejectReason, Is.EqualTo(expected));
        Assert.That(TestBoards.Rows(board), Is.EqualTo(rows));
    }

    [Test]
    public void SwapThatMatches_ClearsScoresAndRefillsInOrder()
    {
        var board = TestBoards.Parse(
            "RGR",
            "BRY",
            "YBG");
        TestBoards.Force(board, "GYB");
        var game = TestBoards.Game(board);

        var result = game.TrySwap(new GridPos(1, 0), new GridPos(1, 1), TestBoards.LastMove);

        Assert.That(result.Outcome, Is.EqualTo(TurnOutcome.Resolved));
        Assert.That(result.Steps.Select(s => s.GetType()), Is.EqualTo(new[]
        {
            typeof(SwapStep), typeof(MoveUsedStep), typeof(MatchPopStep), typeof(ClearStep), typeof(GravityStep)
        }));
        Assert.That(((MoveUsedStep)result.Steps[1]).ByMatch, Is.True);
        Assert.That(TestBoards.Rows(board), Is.EqualTo(new[] { "GYB", "BGY", "YBG" }));
        Assert.That(game.Session.Points, Is.EqualTo(3));
        Assert.That(game.Session.MatchedAnimals, Is.EqualTo(3));
    }

    [Test]
    public void GravityStep_CarriesTheBoardAsItWasThen()
    {
        var board = TestBoards.Parse(
            "RGR",
            "BRY",
            "YBG");
        TestBoards.Force(board, "GYB");

        var result = TestBoards.Game(board).TrySwap(new GridPos(1, 0), new GridPos(1, 1), TestBoards.LastMove);
        var gravity = result.Steps.OfType<GravityStep>().Single();

        Assert.That(gravity.After.TypeAt(new GridPos(0, 0)), Is.EqualTo(TestBoards.TypeOf('G')));
        Assert.That(gravity.Spawns.Count, Is.EqualTo(3));
        Assert.That(gravity.Duration, Is.EqualTo(0.2f));
    }

    [Test]
    public void MatchNextToBone_BreaksItForNoPoints()
    {
        var board = TestBoards.Parse(
            "RRRX",
            "GBYB",
            "BGBG");
        TestBoards.Force(board, "GYGR");
        var game = TestBoards.Game(board);

        var result = game.ResolveBoard(new TurnContext(0, 10, false));

        var broken = result.Steps.OfType<BonesBrokenStep>().Single();
        Assert.That(broken.Cells, Is.EqualTo(new[] { new GridPos(3, 0) }));
        Assert.That(game.Session.Points, Is.EqualTo(3));
        Assert.That(game.Session.CollectedCount("bone"), Is.EqualTo(1));
        Assert.That(TestBoards.Rows(board)[0], Is.EqualTo("GYGR"));
    }

    [Test]
    public void SettledBoard_WolfEatsTheSheepNextToIt()
    {
        var board = TestBoards.Parse(
            "WSG",
            "GBR",
            "BRY");
        var game = TestBoards.Game(board);

        var result = game.ResolveBoard(new TurnContext(0, 10, false));

        var feast = result.Steps.OfType<FeastStep>().Single();
        Assert.That(feast.Meals.Single().Predator, Is.EqualTo(new GridPos(0, 0)));
        Assert.That(feast.Meals.Single().Prey, Is.EqualTo(new GridPos(1, 0)));
        Assert.That(TestBoards.Rows(board)[0], Is.EqualTo("WXG"));
        Assert.That(game.Session.Points, Is.EqualTo(2));
        Assert.That(game.Session.CollectedCount("sheep"), Is.EqualTo(1));
    }

    [Test]
    public void BlackSheepSwipedVertically_BlastsItsRow()
    {
        var board = TestBoards.Parse(
            "RGB",
            "YKR",
            "BRG");
        TestBoards.Force(board, "GBY");
        var game = TestBoards.Game(board);

        var result = game.TrySwap(new GridPos(1, 1), new GridPos(1, 0), TestBoards.LastMove);

        Assert.That(result.Steps.Select(s => s.GetType()), Is.EqualTo(new[]
        {
            typeof(SwapStep), typeof(MoveUsedStep), typeof(BlackSheepBlastStep), typeof(ClearStep), typeof(GravityStep)
        }));
        var blast = (BlackSheepBlastStep)result.Steps[2];
        Assert.That(blast.Center, Is.EqualTo(new GridPos(1, 0)));
        Assert.That(blast.Waves.Count, Is.EqualTo(2), "Centre wave, then one cell each side.");
        Assert.That(((MoveUsedStep)result.Steps[1]).ByMatch, Is.False);
        Assert.That(((ClearStep)result.Steps[3]).Cause, Is.EqualTo(ClearCause.Explosion));
        Assert.That(TestBoards.Rows(board), Is.EqualTo(new[] { "GBY", "YGR", "BRG" }));
        Assert.That(game.Session.Points, Is.EqualTo(7), "Two animals (1 each) and the black sheep itself (5).");
    }

    [Test]
    public void ReachingTheGoal_StopsTheTurnBeforeSettledRules()
    {
        var board = TestBoards.Parse(
            "RRRW",
            "GBYS",
            "BGBG");
        TestBoards.Force(board, "GYG");
        var game = TestBoards.Game(board, new LevelGoals(GoalKind.Points, 3, null));

        var result = game.ResolveBoard(new TurnContext(5, 10, false));

        Assert.That(result.GoalsComplete, Is.True);
        Assert.That(result.Steps.OfType<FeastStep>(), Is.Empty, "The wolf doesn't act after the level is won.");
        Assert.That(game.Session.Points, Is.EqualTo(3));
    }

    [Test]
    public void BoosterClear_SkipsDuplicatesAndBonesButBreaksAdjacentBones()
    {
        var board = TestBoards.Parse(
            "RGX",
            "GBR",
            "BRG");
        TestBoards.Force(board, "BYB");
        var game = TestBoards.Game(board);
        var cells = new[] { new GridPos(0, 0), new GridPos(0, 0), new GridPos(2, 0), new GridPos(1, 0) };

        var result = game.ClearCells(cells, new TurnContext(0, 10, false));

        var clear = result.Steps.OfType<ClearStep>().First();
        Assert.That(clear.Cleared.Count, Is.EqualTo(2));
        Assert.That(result.Steps.OfType<BonesBrokenStep>().Single().Cells, Is.EqualTo(new[] { new GridPos(2, 0) }));
        Assert.That(result.Steps.OfType<GravityStep>().First().Duration, Is.EqualTo(0.1f));
        Assert.That(TestBoards.Rows(board)[0], Is.EqualTo("BYB"));
    }

    [Test]
    public void DeadBoard_IsShuffledIntoAPlayableOne()
    {
        var board = TestBoards.Parse(
            "RBGY",
            "GYRB",
            "BRYG",
            "YGBR");
        Assume.That(MoveFinder.HasAnyMove(board), Is.False, "Fixture must start with no legal move.");

        var result = TestBoards.Game(board, seed: 7).EnsurePlayable(new TurnContext(5, 10, false));

        Assert.That(result.Steps.OfType<ShuffleStep>().Count(), Is.EqualTo(1));
        Assert.That(MatchFinder.HasAnyMatch(board), Is.False);
        Assert.That(MoveFinder.HasAnyMove(board), Is.True);
    }

    [Test]
    public void RulesThatTriggerEachOtherForever_AreStopped()
    {
        var board = TestBoards.Parse("RGB", "GBR", "BRG");
        var game = new Match3Game(board, new GameSession(TestBoards.NoGoal()), new SeededRandomSource(1),
            new IActionRule[] { new EndlessRule() }, SpecialSpawnSettings.None);

        Assert.Throws<InvalidOperationException>(() => game.ResolveBoard(new TurnContext(0, 10, false)));
    }

    private sealed class EndlessRule : ActionRule<BoardSettledAction>
    {
        protected override void React(TurnRun run, BoardSettledAction applied) => run.Enqueue(new BoardSettledAction());
    }
}
