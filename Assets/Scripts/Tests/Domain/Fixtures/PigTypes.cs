using System;
using FuzzyFrenzy.Domain;

internal sealed class PigTypes
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
