using System;
using UnityEngine;

public class BoardModelInitializer
{
    private readonly Board _board;

    public BoardModelInitializer(Board board)
    {
        _board = board;
    }

    public void Initialize()
    {
        if (_board._allowedAnimals == null || _board._allowedAnimals.Count == 0)
            throw new InvalidOperationException("Board has no allowed animals. Check BoardConfig.");

        const int MaxPlacementAttempts = 20;

        // Fill the grid with animals
        for (int x = 0; x < _board._width; x++) // Columns
        {
            for (int y = 0; y < _board._height; y++) // Rows
            {
                var cell = new Vector2Int(x, y);

                int attempts = 0;
                Animal chosen;

                do
                {
                    chosen = _board.PickRandomAllowedAnimal(); // Create a board of random animals
                    attempts++;
                }
                while (_board.MatchFinder.WouldCreateInitialMatches(cell, chosen) && attempts < MaxPlacementAttempts);

                _board._grid[x, y] = chosen;
            }
        }

        if (_board._allowedAnimals.Contains(_board._boneBlock) && _board._boneBlock != null)
            _board._allowedAnimals.Remove(_board._boneBlock);

        if (_board._blackSheep != null)
            _board._allowedAnimals.Remove(_board._blackSheep);

        _board.SpecialPieces.FixStartWolfSheepAdjacency(); // Make sure sheep don't appear next to wolves at the start of the level
    }
}
