using System.Collections.Generic;
using FuzzyFrenzy.Domain;
using UnityEngine;

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
