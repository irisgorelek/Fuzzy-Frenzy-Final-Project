using UnityEngine;

/// <summary>Read-only animals by cell. The live board and a mid-turn snapshot both provide this, so the view can draw either.</summary>
public interface IAnimalGrid
{
    int Width { get; }
    int Height { get; }
    Animal GetAnimalFromCell(Vector2Int cell);
}

