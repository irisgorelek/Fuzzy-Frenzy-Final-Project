using System;
using System.Linq;
using FuzzyFrenzy.Domain;
using NUnit.Framework;

/// <summary>
/// Tests the goose's permanent-wake ability through the real clear/rule/action pipeline.
/// </summary>
public class GoosePermanentWakeTests
{
    private static readonly TurnContext NoShuffle =
        new TurnContext(movesLeft: 0, maxMoves: 0, timerActive: false);

    [TestCase("cycling-pig-awake")]
    [TestCase("cycling-pig-sleep-1")]
    [TestCase("cycling-pig-sleep-2")]
    public void MatchedGoose_PermanentlyWakesAdjacentCyclingPig(string startingPigId)
    {
        var fixture = new GooseMatchFixture(startingPigId, new GridPos(1, 1));
        int startingType = fixture.Board.TypeAt(fixture.PigCell);

        TurnResult result = fixture.Game.ResolveBoard(NoShuffle);

        Assert.That(fixture.Board.TypeAt(fixture.PigCell), Is.EqualTo(fixture.Types.NormalPig));

        TileFormChange change = result.Steps
            .OfType<TileFormsChangedStep>()
            .SelectMany(step => step.Changes)
            .Single(item => item.Cell == fixture.PigCell);

        Assert.That(change.OldType, Is.EqualTo(startingType));
        Assert.That(change.NewType, Is.EqualTo(fixture.Types.NormalPig));
    }

    [Test]
    public void PermanentWake_PreservesThePigRuntimeId()
    {
        var fixture = new GooseMatchFixture("cycling-pig-sleep-1", new GridPos(1, 1));
        int pigId = fixture.Board.Get(fixture.PigCell).Id;

        fixture.Game.ResolveBoard(NoShuffle);

        Assert.That(fixture.Board.Get(fixture.PigCell).Id, Is.EqualTo(pigId));
        Assert.That(fixture.Board.TypeAt(fixture.PigCell), Is.EqualTo(fixture.Types.NormalPig));
    }

    [Test]
    public void MatchedGoose_DoesNotWakeDiagonallyAdjacentPig()
    {
        // The pig is diagonal to the rightmost matched goose, but not orthogonally adjacent to any goose.
        var fixture = new GooseMatchFixture("cycling-pig-sleep-1", new GridPos(3, 1));
        int sleepingType = fixture.Board.TypeAt(fixture.PigCell);

        TurnResult result = fixture.Game.ResolveBoard(NoShuffle);

        Assert.That(fixture.Board.TypeAt(fixture.PigCell), Is.EqualTo(sleepingType));
        bool pigChanged = result.Steps
            .OfType<TileFormsChangedStep>()
            .SelectMany(step => step.Changes)
            .Any(change => change.Cell == fixture.PigCell);
        Assert.That(pigChanged, Is.False);
    }

    [Test]
    public void BoosterClearedGoose_DoesNotWakeAdjacentPig()
    {
        var types = new GooseTypes();
        var board = new BoardState(types.Catalog, 2, 1);
        var gooseCell = new GridPos(0, 0);
        var pigCell = new GridPos(1, 0);
        board.Place(gooseCell, types.Goose);
        board.Place(pigCell, types.Sleep1Pig);
        board.SpawnPool.Add(types.A);
        board.ForcedSpawns.Enqueue(types.A);
        int sleepingType = board.TypeAt(pigCell);
        var game = NewGame(board);

        TurnResult result = game.ClearCells(new[] { gooseCell }, NoShuffle);

        Assert.That(board.TypeAt(pigCell), Is.EqualTo(sleepingType));
        Assert.That(result.Steps.OfType<TileFormsChangedStep>(), Is.Empty,
            "The goose ability activates on a match clear, not on a booster clear.");
    }

    [Test]
    public void MatchedGoose_DoesNotChangeAdjacentNormalPig()
    {
        var fixture = new GooseMatchFixture("pig", new GridPos(1, 1));
        int pigId = fixture.Board.Get(fixture.PigCell).Id;

        TurnResult result = fixture.Game.ResolveBoard(NoShuffle);

        Assert.That(fixture.Board.TypeAt(fixture.PigCell), Is.EqualTo(fixture.Types.NormalPig));
        Assert.That(fixture.Board.Get(fixture.PigCell).Id, Is.EqualTo(pigId));
        Assert.That(result.Steps.OfType<TileFormsChangedStep>(), Is.Empty,
            "An already permanent pig has no CanBePermanentlyAwakened trait.");
    }

    internal static Match3Game NewGame(BoardState board) =>
        new Match3Game(
            board,
            new GameSession(TestBoards.NoGoal()),
            new SeededRandomSource(1),
            Match3Game.DefaultRules(),
            SpecialSpawnSettings.None);
}
