using FuzzyFrenzy.Domain;
using UnityEngine;

/// <summary>A board snapshot from a turn step, shown as animals.</summary>
public sealed class SnapshotAnimalGrid : IAnimalGrid
{
    private readonly BoardSnapshot _snapshot;
    private readonly AnimalCatalog _catalog;

    public SnapshotAnimalGrid(BoardSnapshot snapshot, AnimalCatalog catalog)
    {
        _snapshot = snapshot;
        _catalog = catalog;
    }

    public int Width => _snapshot.Width;
    public int Height => _snapshot.Height;

    public Animal GetAnimalFromCell(Vector2Int cell) => _catalog.AnimalOf(_snapshot.TypeAt(cell.ToGrid()));
}
