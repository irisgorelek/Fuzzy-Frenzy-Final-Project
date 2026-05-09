using System.Collections.Generic;
using UnityEngine;

public class BoardModelCellClearer
{
    private readonly Board _board;

    public BoardModelCellClearer(Board board)
    {
        _board = board;
    }

    // Clear the found matches
    public void ClearMatches(List<Vector2Int> matches, List<Board.FallMove> fallMoves = null, List<Board.SpawnInfo> spawns = null)
    {
        var destroyedByAnimal = new Dictionary<string, int>();
        int pointsGainedThisClear = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            var a = _board._grid[matches[i].x, matches[i].y];
            if (a == null) continue;
            if (_board.IsCellLocked(matches[i])) continue;

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFXPitchAdjusted(8, 0.2f); // Play pop sound.
            }

            _board._points += a._points;
            pointsGainedThisClear += a._points;
            _board._matchedAnimals++;

            string animalId = a._id;

            if (destroyedByAnimal.ContainsKey(animalId))
                destroyedByAnimal[animalId]++;
            else
                destroyedByAnimal[animalId] = 1;

            _board._grid[matches[i].x, matches[i].y] = null;
        }

        _board.SpecialPieces.DamageAdjacentBoneBlocks(matches);

        if (pointsGainedThisClear > 0)
            _board.OnScoreAdded?.Invoke(pointsGainedThisClear);

        foreach (var kvp in destroyedByAnimal)
        {
            _board.OnAnimalsDestroyed?.Invoke(kvp.Key, kvp.Value);
        }

        _board.ApplyGravity(fallMoves);
        _board.Refill(spawns);

        //ResolveWolfSheepInteractions(fallMoves, spawns);
    }

    public void ClearCells(IEnumerable<Vector2Int> cells, List<Board.FallMove> fallMoves = null, List<Board.SpawnInfo> spawns = null)
    {
        if (cells == null)
            return;

        var uniqueCells = new HashSet<Vector2Int>(cells);
        var destroyedByAnimal = new Dictionary<string, int>();
        var clearedCells = new List<Vector2Int>();
        int pointsGainedThisClear = 0;

        foreach (var cell in uniqueCells)
        {
            if (!_board.IsCellInBounds(cell))
                continue;

            var a = _board._grid[cell.x, cell.y];
            if (a == null)
                continue;
            if (_board.IsCellLocked(cell))
                continue;

            // don't count / clear bone blocks
            if (_board._boneBlock != null && a == _board._boneBlock)
                continue;

            _board._points += a._points;
            pointsGainedThisClear += a._points;
            _board._matchedAnimals++;

            if (!string.IsNullOrEmpty(a._id))
            {
                if (destroyedByAnimal.ContainsKey(a._id))
                    destroyedByAnimal[a._id]++;
                else
                    destroyedByAnimal[a._id] = 1;
            }

            _board._grid[cell.x, cell.y] = null;
            clearedCells.Add(cell);
        }

        _board.SpecialPieces.DamageAdjacentBoneBlocks(clearedCells);

        if (pointsGainedThisClear > 0)
            _board.OnScoreAdded?.Invoke(pointsGainedThisClear);

        foreach (var kvp in destroyedByAnimal)
            _board.OnAnimalsDestroyed?.Invoke(kvp.Key, kvp.Value);

        _board.ApplyGravity(fallMoves);
        _board.Refill(spawns);
        //ResolveWolfSheepInteractions(fallMoves, spawns);
    }

    public void ClearCellsAsExplosion(List<Vector2Int> cells)
    {
        var destroyedByAnimal = new Dictionary<string, int>();
        int pointsGainedThisClear = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];
            var a = _board._grid[c.x, c.y];
            if (a == null) continue;
            if (_board.IsCellLocked(c)) continue;

            // Dont clear bone blocks
            if (_board._boneBlock != null && a == _board._boneBlock)
                continue;

            _board._points += a._points;
            pointsGainedThisClear += a._points;
            _board._matchedAnimals++;

            string animalId = a._id;
            if (destroyedByAnimal.ContainsKey(animalId)) destroyedByAnimal[animalId]++;
            else destroyedByAnimal[animalId] = 1;

            _board._grid[c.x, c.y] = null;
        }

        // destroys bones adjacent to the explosion - Can delete if we don't want that
        _board.SpecialPieces.DamageAdjacentBoneBlocks(cells);

        if (pointsGainedThisClear > 0)
            _board.OnScoreAdded?.Invoke(pointsGainedThisClear);

        foreach (var kvp in destroyedByAnimal)
            _board.OnAnimalsDestroyed?.Invoke(kvp.Key, kvp.Value);

        _board.ApplyGravity();
        _board.Refill();
        //ResolveWolfSheepInteractions();
    }
}
