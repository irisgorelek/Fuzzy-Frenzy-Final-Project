using System;
using System.Text;
using FuzzyFrenzy.Domain;

/// <summary>
/// Builds core boards from strings, one string per row, top row first.
/// R G B Y P = normal animals, W = wolf, S = sheep, X = bone, K = black sheep, . = empty.
/// </summary>
internal static class TestBoards
{
    private const string Letters = "RGBYPWSXK";

    public static BoardState Parse(params string[] rows)
    {
        if (rows == null || rows.Length == 0) throw new ArgumentException("Give at least one row.");

        int width = rows[0].Length;
        var catalog = new TileCatalog();
        foreach (char letter in Letters)
            catalog.Add(Definition(letter));

        var board = new BoardState(catalog, width, rows.Length);
        foreach (char letter in "RGBY")
            board.SpawnPool.Add(TypeOf(letter));

        for (int y = 0; y < rows.Length; y++)
        {
            if (rows[y].Length != width) throw new ArgumentException($"Row {y} has a different width.");
            for (int x = 0; x < width; x++)
                board.Place(new GridPos(x, y), rows[y][x] == '.' ? -1 : TypeOf(rows[y][x]));
        }

        return board;
    }

    public static int TypeOf(char letter)
    {
        int type = Letters.IndexOf(letter);
        if (type < 0) throw new ArgumentException($"Unknown tile letter '{letter}'.");
        return type;
    }

    /// <summary>Queues refills so a test knows exactly which tiles enter.</summary>
    public static void Force(BoardState board, string letters)
    {
        foreach (char letter in letters)
            board.ForcedSpawns.Enqueue(TypeOf(letter));
    }

    public static string[] Rows(BoardState board)
    {
        var rows = new string[board.Height];
        for (int y = 0; y < board.Height; y++)
        {
            var sb = new StringBuilder(board.Width);
            for (int x = 0; x < board.Width; x++)
            {
                int type = board.TypeAt(new GridPos(x, y));
                sb.Append(type < 0 ? '.' : Letters[type]);
            }
            rows[y] = sb.ToString();
        }
        return rows;
    }

    public static Match3Game Game(BoardState board, LevelGoals goals = null, int seed = 1) =>
        new Match3Game(board, new GameSession(goals ?? NoGoal()), new SeededRandomSource(seed),
            Match3Game.DefaultRules(), SpecialSpawnSettings.None);

    /// <summary>A goal the tests never reach, so turns always play out fully.</summary>
    public static LevelGoals NoGoal() => new LevelGoals(GoalKind.Points, int.MaxValue, null);

    /// <summary>One move left: the turn uses it, so the dead-board shuffle never kicks in.</summary>
    public static TurnContext LastMove => new TurnContext(movesLeft: 1, maxMoves: 10, timerActive: false);

    private static TileDefinition Definition(char letter)
    {
        switch (letter)
        {
            case 'X': return new TileDefinition("bone", 0, canMatch: false, canSwap: false, affectedByGravity: false, spawnWeight: 0f,
                TileTraits.BreaksNextToClear | TileTraits.ResistsBlasts | TileTraits.NoRefill);
            case 'K': return new TileDefinition("blackSheep", 5, canMatch: false, canSwap: true, affectedByGravity: true, spawnWeight: 0f,
                TileTraits.LineBlastOnSwap | TileTraits.NoRefill);
            case 'W': return new TileDefinition("wolf", 1, true, true, true, 1f, eatsId: "sheep", leavesId: "bone");
            case 'S': return new TileDefinition("sheep", 2, true, true, true, 1f);
            default: return new TileDefinition(letter.ToString(), 1, true, true, true, 1f);
        }
    }
}
