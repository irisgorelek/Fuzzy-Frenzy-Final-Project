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

    private sealed class PigTurnFixture
    {
        public static readonly TurnContext LastMove =
            new TurnContext(movesLeft: 1, maxMoves: 10, timerActive: false);

        public PigTurnFixture(string startingPigId, string forcedSpawns)
        {
            Types = new PigTypes();
            Board = new BoardState(Types.Catalog, 3, 3);
            PigCell = new GridPos(0, 2);

            // Swapping B at (1,0) with A at (1,1) creates AAA across the top row.
            int[,] rows =
            {
                { Types.A, Types.B, Types.A },
                { Types.C, Types.A, Types.D },
                { Types.Catalog.TypeOfId(startingPigId), Types.C, Types.B },
            };

            for (int y = 0; y < Board.Height; y++)
                for (int x = 0; x < Board.Width; x++)
                    Board.Place(new GridPos(x, y), rows[y, x]);

            Board.SpawnPool.AddRange(new[] { Types.A, Types.B, Types.C, Types.D });
            foreach (char letter in forcedSpawns)
                Board.ForcedSpawns.Enqueue(Types.TypeOfNormal(letter));

            Game = new Match3Game(
                Board,
                new GameSession(TestBoards.NoGoal()),
                new SeededRandomSource(1),
                Match3Game.DefaultRules(),
                SpecialSpawnSettings.None);
        }

        public PigTypes Types { get; }
        public BoardState Board { get; }
        public Match3Game Game { get; }
        public GridPos PigCell { get; }

        public TurnResult PlayMatchingTurn() => Game.TrySwap(
            new GridPos(1, 0),
            new GridPos(1, 1),
            LastMove);
    }

    private sealed class PigTypes
    {
        public PigTypes()
        {
            NormalPig = Catalog.Add(new TileDefinition(
                "pig", 1, true, true, true, 1f,
                matchGroupId: "pig"));

            AwakePig = Catalog.Add(new TileDefinition(
                "cycling-pig-awake", 1, true, true, true, 0f,
                TileTraits.AdvancesFormEachTurn | TileTraits.CanBePermanentlyAwakened,
                matchGroupId: "pig",
                nextTurnFormId: "cycling-pig-sleep-2",
                permanentAwakeFormId: "pig"));

            Sleep2Pig = Catalog.Add(new TileDefinition(
                "cycling-pig-sleep-2", 1, false, false, true, 0f,
                TileTraits.AdvancesFormEachTurn | TileTraits.CanBePermanentlyAwakened,
                matchGroupId: "pig",
                nextTurnFormId: "cycling-pig-sleep-1",
                permanentAwakeFormId: "pig"));

            Sleep1Pig = Catalog.Add(new TileDefinition(
                "cycling-pig-sleep-1", 1, false, false, true, 0f,
                TileTraits.AdvancesFormEachTurn | TileTraits.CanBePermanentlyAwakened,
                matchGroupId: "pig",
                nextTurnFormId: "cycling-pig-awake",
                permanentAwakeFormId: "pig"));

            A = AddNormal("a");
            B = AddNormal("b");
            C = AddNormal("c");
            D = AddNormal("d");
        }

        public TileCatalog Catalog { get; } = new TileCatalog();
        public int NormalPig { get; }
        public int AwakePig { get; }
        public int Sleep2Pig { get; }
        public int Sleep1Pig { get; }
        public int A { get; }
        public int B { get; }
        public int C { get; }
        public int D { get; }

        public int TypeOfNormal(char letter)
        {
            switch (letter)
            {
                case 'A': return A;
                case 'B': return B;
                case 'C': return C;
                case 'D': return D;
                default: throw new ArgumentException($"Unknown normal tile '{letter}'.");
            }
        }

        private int AddNormal(string id) =>
            Catalog.Add(new TileDefinition(id, 1, true, true, true, 1f));
    }
}
