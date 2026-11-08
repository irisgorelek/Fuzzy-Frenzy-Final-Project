using FuzzyFrenzy.Domain;

internal sealed class GooseTypes
{
    public GooseTypes()
    {
        NormalPig = Catalog.Add(new TileDefinition(
            "pig", 1, true, true, true, 1f,
            matchGroupId: "pig"));

        AwakePig = Catalog.Add(new TileDefinition(
            "cycling-pig-awake", 1, true, true, true, 0f,
            TileTraits.AdvancesFormEachTurn | TileTraits.CanBePermanentlyAwakened,
            matchGroupId: "pig",
            nextTurnFormId: "cycling-pig-sleep-1",
            permanentAwakeFormId: "pig"));

        Sleep1Pig = Catalog.Add(new TileDefinition(
            "cycling-pig-sleep-1", 1, false, false, true, 0f,
            TileTraits.AdvancesFormEachTurn | TileTraits.CanBePermanentlyAwakened,
            matchGroupId: "pig",
            nextTurnFormId: "cycling-pig-sleep-2",
            permanentAwakeFormId: "pig"));

        Sleep2Pig = Catalog.Add(new TileDefinition(
            "cycling-pig-sleep-2", 1, false, false, true, 0f,
            TileTraits.AdvancesFormEachTurn | TileTraits.CanBePermanentlyAwakened,
            matchGroupId: "pig",
            nextTurnFormId: "cycling-pig-awake",
            permanentAwakeFormId: "pig"));

        Goose = Catalog.Add(new TileDefinition(
            "goose", 1, true, true, true, 1f,
            TileTraits.PermanentlyWakesAdjacent));

        A = AddNormal("goose-test-a");
        B = AddNormal("goose-test-b");
        C = AddNormal("goose-test-c");
        D = AddNormal("goose-test-d");
    }

    public TileCatalog Catalog { get; } = new TileCatalog();
    public int NormalPig { get; }
    public int AwakePig { get; }
    public int Sleep1Pig { get; }
    public int Sleep2Pig { get; }
    public int Goose { get; }
    public int A { get; }
    public int B { get; }
    public int C { get; }
    public int D { get; }

    private int AddNormal(string id) =>
        Catalog.Add(new TileDefinition(id, 1, true, true, true, 1f));
}
