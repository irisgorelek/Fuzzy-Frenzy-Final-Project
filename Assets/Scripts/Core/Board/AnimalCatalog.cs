using System.Collections.Generic;
using FuzzyFrenzy.Domain;
using UnityEngine;

/// <summary>
/// Maps Animal assets to the core's tile types and back. This is the boundary where Unity data becomes core data.
/// Each distinct Animal asset gets its own type, so two assets never count as the same tile.
/// </summary>
public sealed class AnimalCatalog
{
    private readonly List<Animal> _animals = new List<Animal>();
    private readonly Dictionary<Animal, int> _types = new Dictionary<Animal, int>();

    public TileCatalog Tiles { get; } = new TileCatalog();

    /// <summary>
    /// The tile type for an animal, registering it on first use. Null maps to an empty cell (-1).
    /// Registering a predator also registers what it eats and leaves behind, so its remains can appear.
    /// </summary>
    public int TypeOf(Animal animal)
    {
        if (animal == null)
            return -1;

        if (_types.TryGetValue(animal, out int type))
            return type;

        var definition = new TileDefinition(
            animal._id,
            animal._points,
            animal._canMatch,
            animal._canSwap,
            animal._affectedByGravity,
            animal._spawnWeight,
            animal._traits,
            animal._eats != null ? animal._eats._id : null,
            animal._leavesBehind != null ? animal._leavesBehind._id : null,
            animal._matchGroupId,
            animal._nextTurnForm != null ? animal._nextTurnForm._id : null,
            animal._permanentAwakeForm != null ? animal._permanentAwakeForm._id : null);
            

        type = Tiles.Add(definition);
        _animals.Add(animal);
        _types[animal] = type;

        TypeOf(animal._eats);
        TypeOf(animal._leavesBehind);
        TypeOf(animal._nextTurnForm);
        TypeOf(animal._permanentAwakeForm);
        
        return type;
    }

    public Animal AnimalOf(int type) => type >= 0 && type < _animals.Count ? _animals[type] : null;
}

/// <summary>Uses Unity's global random, so gameplay randomness behaves exactly as before.</summary>
public sealed class UnityRandomSource : IRandomSource
{
    public int Range(int minInclusive, int maxExclusive) => Random.Range(minInclusive, maxExclusive);
    public float Value() => Random.value;
}

public static class GridPosConversions
{
    public static GridPos ToGrid(this Vector2Int v) => new GridPos(v.x, v.y);
    public static Vector2Int ToVector(this GridPos p) => new Vector2Int(p.X, p.Y);

    public static List<Vector2Int> ToVectors(this IReadOnlyList<GridPos> cells)
    {
        var result = new List<Vector2Int>(cells?.Count ?? 0);
        if (cells == null) return result;
        for (int i = 0; i < cells.Count; i++) result.Add(cells[i].ToVector());
        return result;
    }

    public static GridPos[] ToGrid(this IEnumerable<Vector2Int> cells)
    {
        var result = new List<GridPos>();
        if (cells == null) return result.ToArray();
        foreach (var c in cells) result.Add(c.ToGrid());
        return result.ToArray();
    }
}
