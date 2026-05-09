using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardModelGravityResolver
{
    private readonly Board _board;

    public BoardModelGravityResolver(Board board)
    {
        _board = board;
    }

    // Apply gravity to the cells
    public void ApplyGravity(List<Board.FallMove> fallMoves = null)
    {
        for (int x = 0; x < _board._width; x++)
        {
            int writeY = _board._height - 1; // Next slot we can write into

            // Scan from bottom to top
            for (int y = _board._height - 1; y >= 0; y--)
            {
                var piece = _board._grid[x, y];
                if (piece == null)
                    continue;

                if (_board.IsCellLocked(new Vector2Int(x, y)))
                {
                    writeY = y - 1;
                    continue;
                }

                // If bone block, don't apply gravity
                if (!piece._affectedByGravity)
                {
                    writeY = y - 1;
                    continue;
                }

                if (y != writeY)
                {
                    // Skip over obstacles if writeY is (somehow) pointing at one
                    while (writeY >= 0 && _board._grid[x, writeY] != null && !_board._grid[x, writeY]._affectedByGravity)
                        writeY--;

                    if (writeY < 0)
                        break;

                    fallMoves?.Add(new Board.FallMove
                    {
                        from = new Vector2Int(x, y),
                        to = new Vector2Int(x, writeY)
                    });

                    _board._grid[x, writeY] = piece;
                    _board._grid[x, y] = null;
                }

                writeY--;
            }
        }
    }

    // Refill the empty cells
    public void Refill(List<Board.SpawnInfo> spawns = null)
    {
        // Collect empty cells
        var empties = new List<Vector2Int>();
        for (int x = 0; x < _board._width; x++)
            for (int y = 0; y < _board._height; y++)
                if (_board._grid[x, y] == null)
                    empties.Add(new Vector2Int(x, y));

        // If armed, spawn exactly ONE black sheep into an empty spot
        if (_board._blackSheepArmed && _board._blackSheep != null && empties.Count > 0)
        {
            var chosenCell = empties[Random.Range(0, empties.Count)];

            _board._grid[chosenCell.x, chosenCell.y] = _board._blackSheep;

            spawns?.Add(new Board.SpawnInfo
            {
                cell = chosenCell,
                animal = _board._blackSheep,
                spawnFromY = GetSpawnFromY(chosenCell)
            });

            _board._blackSheepArmed = false;
            empties.Remove(chosenCell);
        }

        // Fill remaining empties with normal animals
        foreach (var cell in empties)
        {
            var spawned = _board._debugForcedSpawns.Count > 0 ? _board._debugForcedSpawns.Dequeue() : _board.PickRandomAllowedAnimal(); // Depends on if we use the test board
            _board._grid[cell.x, cell.y] = spawned;

            spawns?.Add(new Board.SpawnInfo
            {
                cell = cell,
                animal = spawned,
                spawnFromY = GetSpawnFromY(cell)
            });
        }
    }

    // Helper for gravaity + bone animation
    private int GetSpawnFromY(Vector2Int target)
    {
        // Search upward for the nearest blocker
        for (int y = target.y - 1; y >= 0; y--)
        {
            var above = _board._grid[target.x, y];
            if (above != null && !above._affectedByGravity)
                return y; // blocker location
        }
        return -1; // no blocker above -> spawn from above board
    }
}
