using System;
using System.Collections.Generic;
using System.Linq;
using FuzzyFrenzy.Domain;
using NUnit.Framework;

/// <summary>
/// Tests the complete cycling-pig path using only the Unity-free domain:
/// shared match groups, form changes, turn timing, identity and gravity.
/// </summary>
public class PigFormCycleTests
{
    [Test]
    public void MatchGroup_AwakePigMatchesNormalPigs_ButSleepingPigDoesNot()
    {
        var types = new PigTypes();
        var board = new BoardState(types.Catalog, 3, 1);

        board.Place(new GridPos(0, 0), types.NormalPig);
        board.Place(new GridPos(1, 0), types.NormalPig);
        board.Place(new GridPos(2, 0), types.AwakePig);

        Assert.That(MatchFinder.HasAnyMatch(board), Is.True,
            "The temporary awake form shares the 'pig' match group with the normal pig.");

        board.ChangeType(new GridPos(2, 0), types.Sleep2Pig);

        Assert.That(MatchFinder.HasAnyMatch(board), Is.False,
            "A sleeping form has the same match group but CanMatch is false.");
    }

    [TestCase("cycling-pig-awake", "cycling-pig-sleep-2")]
    [TestCase("cycling-pig-sleep-2", "cycling-pig-sleep-1")]
    [TestCase("cycling-pig-sleep-1", "cycling-pig-awake")]
    public void ValidTurn_AdvancesExactlyToConfiguredNextForm(string startingId, string expectedId)
    {
        var fixture = new PigTurnFixture(startingId, "BCD");

        TurnResult result = fixture.PlayMatchingTurn();

        Assert.That(result.Outcome, Is.EqualTo(TurnOutcome.Resolved));
        Assert.That(fixture.Board.TypeAt(fixture.PigCell),
            Is.EqualTo(fixture.Types.Catalog.TypeOfId(expectedId)));

        var formStep = result.Steps.OfType<TileFormsChangedStep>().Single();
        Assert.That(formStep.Changes.Count, Is.EqualTo(1));
        Assert.That(formStep.Changes[0].Cell, Is.EqualTo(fixture.PigCell));
        Assert.That(formStep.Changes[0].OldType,
            Is.EqualTo(fixture.Types.Catalog.TypeOfId(startingId)));
        Assert.That(formStep.Changes[0].NewType,
            Is.EqualTo(fixture.Types.Catalog.TypeOfId(expectedId)));
    }

    [Test]
    public void FormChange_PreservesTheRuntimeTileId()
    {
        var fixture = new PigTurnFixture("cycling-pig-awake", "BCD");
        int originalTileId = fixture.Board.Get(fixture.PigCell).Id;

        fixture.PlayMatchingTurn();

        Assert.That(fixture.Board.Get(fixture.PigCell).Id, Is.EqualTo(originalTileId),
            "Changing form changes the tile type, not the identity of that pig instance.");
    }

    [Test]
    public void NoMatchSwap_DoesNotAdvanceThePigForm()
    {
        var fixture = new PigTurnFixture("cycling-pig-awake", "BCD");
        int originalType = fixture.Board.TypeAt(fixture.PigCell);

        TurnResult result = fixture.Game.TrySwap(
            new GridPos(0, 1),
            new GridPos(1, 1),
            PigTurnFixture.LastMove);

        Assert.That(result.Outcome, Is.EqualTo(TurnOutcome.NoMatch));
        Assert.That(fixture.Board.TypeAt(fixture.PigCell), Is.EqualTo(originalType));
        Assert.That(result.Steps.OfType<TileFormsChangedStep>(), Is.Empty);
    }

    [Test]
    public void SleepingPig_CannotBeSwapped()
    {
        var fixture = new PigTurnFixture("cycling-pig-sleep-2", "BCD");

        TurnResult result = fixture.Game.TrySwap(
            fixture.PigCell,
            new GridPos(1, 2),
            PigTurnFixture.LastMove);

        Assert.That(result.Outcome, Is.EqualTo(TurnOutcome.Rejected));
        Assert.That(result.RejectReason, Is.EqualTo(RejectReason.NotSwappable));
        Assert.That(fixture.Board.TypeAt(fixture.PigCell), Is.EqualTo(fixture.Types.Sleep2Pig));
    }

    [Test]
    public void SleepingPig_IsStillAffectedByGravity()
    {
        var types = new PigTypes();
        var board = new BoardState(types.Catalog, 1, 3);
        var start = new GridPos(0, 0);
        var destination = new GridPos(0, 2);
        board.Place(start, types.Sleep2Pig);
        int originalTileId = board.Get(start).Id;
        var falls = new List<FallMove>();

        GravitySolver.Apply(board, falls);

        Assert.That(board.IsEmpty(start), Is.True);
        Assert.That(board.TypeAt(destination), Is.EqualTo(types.Sleep2Pig));
        Assert.That(board.Get(destination).Id, Is.EqualTo(originalTileId));
        Assert.That(falls.Single().From, Is.EqualTo(start));
        Assert.That(falls.Single().To, Is.EqualTo(destination));
    }

    [Test]
    public void MultipleCascades_AdvanceThePigOnlyOnce()
    {
        // The first refill creates another AAA row, forcing a second clear. BCD then ends the cascade.
        var fixture = new PigTurnFixture("cycling-pig-awake", "AAABCD");

        TurnResult result = fixture.PlayMatchingTurn();

        Assert.That(result.Steps.OfType<MatchPopStep>().Count(), Is.EqualTo(2),
            "The fixture must really contain two cascade clears.");
        Assert.That(result.Steps.OfType<TileFormsChangedStep>().Count(), Is.EqualTo(1),
            "All cascades belong to one player turn.");
        Assert.That(fixture.Board.TypeAt(fixture.PigCell), Is.EqualTo(fixture.Types.Sleep2Pig),
            "The pig advances Awake -> Sleep2, not Awake -> Sleep2 -> Sleep1.");
    }
}
