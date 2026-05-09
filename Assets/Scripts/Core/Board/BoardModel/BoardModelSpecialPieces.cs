using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardModelSpecialPieces
{
    private readonly Board _board;

    public BoardModelSpecialPieces(Board board)
    {
        _board = board;
    }

    public void DamageAdjacentBoneBlocks(List<Vector2Int> matches)
    {
        if (_board._boneBlock == null || matches == null || matches.Count == 0)
            return;

        var toRemove = new HashSet<Vector2Int>();

        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            for (int d = 0; d < Board.OrthogonalDirs.Length; d++)
            {
                var n = m + Board.OrthogonalDirs[d];
                if (!_board.IsCellInBounds(n)) continue;

                if (_board._grid[n.x, n.y] == _board._boneBlock)
                    toRemove.Add(n);
            }
        }

        foreach (var cell in toRemove) // HERE //
        {
            _board._grid[cell.x, cell.y] = null;
            _board.OnAnimalsDestroyed?.Invoke(_board._boneBlock._id, 1);
        }
    }

    // ----- Wolf -> Sheep interaction ----- //
    public void ResolveWolfSheepAfterCascades()
    {
        if (_board._wolf == null || _board._sheep == null || _board._boneBlock == null)
            return;

        bool changed;
        int safety = 0;

        do
        {
            changed = ResolveWolfSheepOnce(out int eatenCount, out int pointsGained);

            if (changed)
            {
                if (pointsGained > 0)
                    _board.OnScoreAdded?.Invoke(pointsGained);

                if (eatenCount > 0)
                    _board.OnAnimalsDestroyed?.Invoke(_board._sheep._id, eatenCount);
            }
        }
        while (changed && safety++ < 100);
    }

    private void ResolveWolfSheepInteractions(List<Board.FallMove> fallMoves = null, List<Board.SpawnInfo> spawns = null)
    {
        if (_board._wolf == null || _board._sheep == null || _board._boneBlock == null)
            return;

        // In case eating creates more falling and more eating, continue until stable
        bool changed;
        int safety = 0;

        do
        {
            changed = ResolveWolfSheepOnce(out int eatenCount, out int pointsGained);

            if (changed)
            {
                if (pointsGained > 0)
                    _board.OnScoreAdded?.Invoke(pointsGained); // Add the sheep in the score

                if (eatenCount > 0)
                    _board.OnAnimalsDestroyed?.Invoke(_board._sheep._id, eatenCount); // Add the sheep as a match

                _board.ApplyGravity(fallMoves);
                _board.Refill(spawns);
            }
        }
        while (changed && safety++ < 100);
    }

    private bool ResolveWolfSheepOnce(out int eatenCount, out int pointsGained)
    {
        eatenCount = 0;
        pointsGained = 0;

        var sheepToEat = new List<(Vector2Int sheep, Vector2Int wolf)>();

        for (int x = 0; x < _board._width; x++)
        {
            for (int y = 0; y < _board._height; y++)
            {
                if (_board._grid[x, y] != _board._sheep)
                    continue;

                var sheepCell = new Vector2Int(x, y);

                for (int d = 0; d < Board.OrthogonalDirs.Length; d++)
                {
                    var wolfCell = sheepCell + Board.OrthogonalDirs[d];
                    if (!_board.IsCellInBounds(wolfCell))
                        continue;

                    if (_board._grid[wolfCell.x, wolfCell.y] == _board._wolf)
                    {
                        sheepToEat.Add((sheepCell, wolfCell));
                        break;
                    }
                }
            }
        }

        if (sheepToEat.Count == 0)
            return false;

        foreach (var pair in sheepToEat)
        {
            var sheepCell = pair.sheep;
            var sheepAnimal = _board._grid[sheepCell.x, sheepCell.y];
            if (sheepAnimal == null)
                continue;

            eatenCount++;
            _board._points += sheepAnimal._points;
            pointsGained += sheepAnimal._points;
            _board._matchedAnimals++;

            // Turn sheep into bone
            _board._grid[sheepCell.x, sheepCell.y] = _board._boneBlock;

            // Tell the controller which wolf and which sheep were involved
            _board.OnWolfAteSheep?.Invoke(pair.wolf, pair.sheep);
        }

        return true;
    }

    public void FixStartWolfSheepAdjacency()
    {
        if (_board._wolf == null || _board._sheep == null) return;

        const int MaxPasses = 50;
        const int MaxAttemptsPerCell = 30;

        for (int pass = 0; pass < MaxPasses; pass++)
        {
            bool changed = false;

            for (int x = 0; x < _board._width; x++)
                for (int y = 0; y < _board._height; y++)
                {
                    if (_board._grid[x, y] != _board._sheep) continue;

                    var cell = new Vector2Int(x, y);
                    if (!HasNeighbor(cell, _board._wolf)) continue;

                    // Reroll this sheep into something else that:
                    // isn't sheep or wolf, doesn't create a 3-match immediately, isn't adjacent to a wolf
                    for (int attempt = 0; attempt < MaxAttemptsPerCell; attempt++)
                    {
                        var candidate = _board.PickRandomAllowedAnimal(); // your weighted picker (or your old random)

                        if (candidate == _board._sheep || candidate == _board._wolf) continue;
                        if (_board.MatchFinder.WouldCreateMatchAnywhere(cell, candidate)) continue;

                        _board._grid[cell.x, cell.y] = candidate;

                        if (HasNeighbor(cell, _board._wolf))
                            continue; // still adjacent, try again

                        changed = true;
                        break;
                    }
                }

            if (!changed)
                break; // stable, no sheep next to wolves
        }
    }

    private bool HasNeighbor(Vector2Int cell, Animal target)
    {
        for (int i = 0; i < Board.OrthogonalDirs.Length; i++)
        {
            var n = cell + Board.OrthogonalDirs[i];
            if (!_board.IsCellInBounds(n)) continue;
            if (_board._grid[n.x, n.y] == target) return true;
        }
        return false;
    }

    // ----- Black Sheep ----- //
    public void RollForBlackSheep(float chance01)
    {
        if (_board._blackSheep == null) return;
        if (_board._blackSheepArmed) return; // already queued

        chance01 = Mathf.Clamp01(chance01);
        if (Random.value < chance01)
            _board._blackSheepArmed = true;
    }

    public void TriggerSheepSwipeBlast(Vector2Int sheepPosAfterSwap, bool swipedVertically)
    {
        if (!_board.IsCellInBounds(sheepPosAfterSwap))
            return;

        // vertical swipe -> ROW
        // horizontal swipe -> COLUMN
        var cellsToClear = new List<Vector2Int>();

        if (swipedVertically)
        {
            int y = sheepPosAfterSwap.y;
            for (int x = 0; x < _board._width; x++)
                cellsToClear.Add(new Vector2Int(x, y));
        }
        else
        {
            int x = sheepPosAfterSwap.x;
            for (int y = 0; y < _board._height; y++)
                cellsToClear.Add(new Vector2Int(x, y));
        }

        _board.CellClearer.ClearCellsAsExplosion(cellsToClear);
    }
}
