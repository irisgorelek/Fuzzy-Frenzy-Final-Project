using System;
using System.Collections.Generic;
using UnityEngine;

public class BoardModelDebugTools
{
    private readonly Board _board;

    public BoardModelDebugTools(Board board)
    {
        _board = board;
    }

    // =================================== TEST ======================================== //
    public void InitializeStaticDeadBoard()
    {
        if (_board._allowedAnimals == null || _board._allowedAnimals.Count == 0)
            throw new InvalidOperationException("Board has no allowed animals. Check BoardConfig.");

        if (_board._width != 5 || _board._height != 5)
            throw new InvalidOperationException("InitializeStaticDeadBoard supports only a 5x5 board.");

        if (_board._wolf == null || _board._sheep == null || _board._boneBlock == null)
            throw new InvalidOperationException("Wolf/Sheep/BoneBlock references are missing in BoardConfig.");

        // Only normal playable animals
        var pool = new List<Animal>();
        foreach (var animal in _board._allowedAnimals)
        {
            if (animal == null) continue;
            if (!animal._canSwap) continue;
            if (!animal._canMatch) continue;

            if (animal == _board._wolf) continue;
            if (animal == _board._sheep) continue;
            if (animal == _board._boneBlock) continue;

            pool.Add(animal);
        }

        if (pool.Count < 3)
            throw new InvalidOperationException("Need at least 3 normal swappable/matchable animals for this test board.");

        Animal A = pool[0];
        Animal B = pool[1];
        Animal C = pool[2];

        // Intended move:
        // swap (3,0) with (4,0)
        //
        // Top row before move: A B B A B
        // Top row after move : A B B B A -> match at x = 1,2,3
        //
        // Forced refill:
        // (1,0) = C
        // (2,0) = C
        // (3,0) = A
        //
        // After cascades, the wolf at (3,1) eats the sheep at (3,2),
        // and then your dead-board shuffle logic should trigger.
        Animal[,] pattern = new Animal[5, 5]
        {
        { A, A, B, B, C },            // x = 0
        { B, A, C, B, C },            // x = 1
        { B, B, C, A, A },            // x = 2
        { A, _board._wolf, _board._sheep, A, A },   // x = 3
        { B, C, C, B, B }             // x = 4
        };

        for (int x = 0; x < _board._width; x++)
        {
            for (int y = 0; y < _board._height; y++)
            {
                _board._grid[x, y] = pattern[x, y];
            }
        }

        _board._blackSheepArmed = false;
        _board._blackSheepArmed = false;

        // Refill the three cleared top cells deterministically
        DebugSetForcedSpawns(C, C, A);

        if (_board.FindMatches().Count > 0)
            throw new InvalidOperationException("Test board is invalid: it contains starting matches.");

        var hintFinder = new BoardHintFinder();
        if (!hintFinder.TryFindHint(_board, out var hint))
            throw new InvalidOperationException("Test board is invalid: it has no legal move.");

        //if (hint.From != new Vector2Int(3, 0) || hint.To != new Vector2Int(4, 0))
        //    throw new InvalidOperationException(
        //        $"Test board is invalid: expected only move (3,0)->(4,0), but got {hint.From}->{hint.To}");

        Debug.Log("3-animal wolf/sheep test ready. Make move: swap (3,0) with (4,0).");
    }

    public void DebugSetForcedSpawns(params Animal[] animals)
    {
        _board._debugForcedSpawns.Clear();

        if (animals == null)
            return;

        for (int i = 0; i < animals.Length; i++)
        {
            if (animals[i] != null)
                _board._debugForcedSpawns.Enqueue(animals[i]);
        }
    }

    // ============================== TEST =============================== //
}
