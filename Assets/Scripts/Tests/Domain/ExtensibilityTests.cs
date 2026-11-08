using System.Collections.Generic;
using System.Linq;
using FuzzyFrenzy.Domain;
using NUnit.Framework;

/// <summary>
/// The seams new animals plug into: behaviour comes from tile data, and rules only see the actions they ask for.
/// Boards are written top row first.
/// </summary>
public class ExtensibilityTests
{
    [Test]
    public void NewPredator_WorksFromDataAlone()
    {
        // A goat that eats bushes and leaves nothing: no new rule or core code.
        var catalog = new TileCatalog();
        int goat = catalog.Add(new TileDefinition("goat", 1, true, true, true, 1f, eatsId: "bush"));
        int bush = catalog.Add(new TileDefinition("bush", 0, false, false, false, 0f, TileTraits.NoRefill));
        int a = catalog.Add(new TileDefinition("a", 1, true, true, true, 1f));
        int b = catalog.Add(new TileDefinition("b", 1, true, true, true, 1f));
        int c = catalog.Add(new TileDefinition("c", 1, true, true, true, 1f));

        var board = new BoardState(catalog, 3, 3);
        int[,] rows =
        {
            { goat, bush, a },
            { b,    c,    a },
            { c,    a,    b },
        };
        for (int y = 0; y < 3; y++)
            for (int x = 0; x < 3; x++)
                board.Place(new GridPos(x, y), rows[y, x]);
        board.SpawnPool.AddRange(new[] { a, b, c });
        board.ForcedSpawns.Enqueue(b);

        var game = new Match3Game(board, new GameSession(TestBoards.NoGoal()), new SeededRandomSource(1),
            Match3Game.DefaultRules(), SpecialSpawnSettings.None);

        var result = game.ResolveBoard(new TurnContext(0, 10, false));

        var feast = result.Steps.OfType<FeastStep>().Single();
        Assert.That(feast.Meals.Single().Predator, Is.EqualTo(new GridPos(0, 0)));
        Assert.That(feast.Meals.Single().Prey, Is.EqualTo(new GridPos(1, 0)));
        Assert.That(game.Session.CollectedCount("bush"), Is.EqualTo(1));
        Assert.That(board.TypeAt(new GridPos(1, 0)), Is.EqualTo(b), "The eaten bush leaves a gap that refills.");
    }

    [Test]
    public void BoneTraits_WorkOnAnyTile()
    {
        // A second obstacle with bone traits breaks next to a match just like the bone.
        var board = TestBoards.Parse(
            "RRRG",
            "GBYB",
            "BYGY");
        var ice = new TileDefinition("ice", 0, false, false, false, 0f, TileTraits.BreaksNextToClear | TileTraits.ResistsBlasts);
        int iceType = board.Catalog.Add(ice);
        board.Place(new GridPos(3, 0), iceType);
        TestBoards.Force(board, "YBGY");

        var game = TestBoards.Game(board);
        var result = game.ResolveBoard(new TurnContext(0, 10, false));

        var broken = result.Steps.OfType<TilesBrokenStep>().First();
        Assert.That(broken.Cells, Is.EqualTo(new[] { new GridPos(3, 0) }));
        Assert.That(broken.Ids, Is.EqualTo(new[] { "ice" }));
        Assert.That(game.Session.CollectedCount("ice"), Is.EqualTo(1));
    }

    [Test]
    public void Rules_OnlySeeTheActionTypeTheyAskFor()
    {
        var swaps = new CountingRule<SwapAction>();
        var everything = new CountingRule<IBoardAction>();
        var board = TestBoards.Parse(
            "RGR",
            "GRG",
            "BYB");
        var game = new Match3Game(board, new GameSession(TestBoards.NoGoal()), new SeededRandomSource(1),
            new IActionRule[] { swaps, everything }, SpecialSpawnSettings.None);

        game.TrySwap(new GridPos(1, 0), new GridPos(1, 1), TestBoards.LastMove);

        Assert.That(swaps.Count, Is.EqualTo(1));
        Assert.That(everything.Count, Is.GreaterThan(1), "A rule for the base type sees every action.");
    }

    [Test]
    public void Generator_KeepsNoRefillTilesOutOfRefills()
    {
        var board = TestBoards.Parse("...", "...", "...");
        board.SpawnPool.Add(TestBoards.TypeOf('X'));

        BoardGenerator.Generate(board, new SeededRandomSource(5));

        Assert.That(board.SpawnPool, Has.No.Member(TestBoards.TypeOf('X')));
        Assert.That(board.SpawnPool, Is.Not.Empty);
    }

    [Test]
    public void Generator_NeverStartsPreyNextToItsPredator()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            var board = TestBoards.Parse("......", "......", "......", "......", "......", "......");
            board.SpawnPool.Clear();
            foreach (char letter in "RGBWS")
                board.SpawnPool.Add(TestBoards.TypeOf(letter));

            BoardGenerator.Generate(board, new SeededRandomSource(seed));

            var meals = new List<Meal>();
            BoardOperations.FindMeals(board, meals);
            Assert.That(meals, Is.Empty, $"seed {seed}");
        }
    }
}
