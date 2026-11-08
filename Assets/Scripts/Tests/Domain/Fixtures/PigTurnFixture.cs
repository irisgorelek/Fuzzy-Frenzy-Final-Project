using FuzzyFrenzy.Domain;

internal sealed class PigTurnFixture
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
