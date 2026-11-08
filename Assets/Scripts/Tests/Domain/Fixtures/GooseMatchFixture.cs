using FuzzyFrenzy.Domain;

internal sealed class GooseMatchFixture
{
    public GooseMatchFixture(string startingPigId, GridPos pigCell)
    {
        Types = new GooseTypes();
        Board = new BoardState(Types.Catalog, 4, 2);
        PigCell = pigCell;

        // Three geese begin matched across the top. ResolveBoard clears them as a match,
        // allowing the test to exercise ClearAction -> PermanentWakeRule -> wake action.
        int[,] rows =
        {
            { Types.Goose, Types.Goose, Types.Goose, Types.A },
            { Types.A, Types.B, Types.C, Types.D },
        };
        rows[PigCell.Y, PigCell.X] = Types.Catalog.TypeOfId(startingPigId);

        for (int y = 0; y < Board.Height; y++)
            for (int x = 0; x < Board.Width; x++)
                Board.Place(new GridPos(x, y), rows[y, x]);

        Board.SpawnPool.AddRange(new[] { Types.A, Types.B, Types.C, Types.D });
        Board.ForcedSpawns.Enqueue(Types.B);
        Board.ForcedSpawns.Enqueue(Types.C);
        Board.ForcedSpawns.Enqueue(Types.D);
        Game = GoosePermanentWakeTests.NewGame(Board);
    }

    public GooseTypes Types { get; }
    public BoardState Board { get; }
    public Match3Game Game { get; }
    public GridPos PigCell { get; }
}
