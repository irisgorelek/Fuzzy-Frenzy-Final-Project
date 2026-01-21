using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class Board
{
    private List<Animal> _allowedAnimals; // Which animals are allowed on the board
    private Animal[,] _grid;
    private int _width;
    private int _height;

    public Board(BoardConfig config)
    {
        _width = config._weidth;
        _height = config._height;

        // Get the allowed animals for the level
        _allowedAnimals = new List<Animal>(config._animals);

        _grid = new Animal[_width, _height];
    }

    public void Initialize()
    {
        if (_allowedAnimals == null || _allowedAnimals.Count == 0)
            throw new InvalidOperationException("Board has no allowed animals. Check BoardConfig.");

        const int MaxPlacementAttempts = 20;

        // Fill the grid with animals
        for (int x = 0; x < _width; x++) // Columns
        {
            for (int y = 0; y < _height; y++) // Rows
            {
                var cell = new Vector2Int(x, y);

                int attempts = 0;
                Animal chosen;

                do
                {
                    chosen = _allowedAnimals[Random.Range(0, _allowedAnimals.Count)];
                    attempts++;
                }
                while (WouldCreateInitialMatches(cell, chosen) && attempts < MaxPlacementAttempts);

                _grid[x, y] = chosen;
            }
        }
    }

    // Try to swap the animals between 2 touching cells
    public void TrySwapCells(Vector2Int cell1, Vector2Int cell2)
    {
        // Swap the cells and check what happens
        if((IsCellInBounds(cell1) && IsCellInBounds(cell2)) && AreCellsNeighbours(cell1, cell2))
        {
            Animal temp = _grid[cell1.x, cell1.y];
            _grid[cell1.x, cell1.y] = _grid[cell2.x, cell2.y];
            _grid[cell2.x, cell2.y] = temp;
        }

        List<Vector2Int> matches = MatchesFound();

        // If there were no matches found return the cells back to what they were
        if (MatchesFound() == null)
        {
            Animal temp = _grid[cell1.x, cell1.y];
            _grid[cell1.x, cell1.y] = _grid[cell2.x, cell2.y];
            _grid[cell2.x, cell2.y] = temp;
            return;
        }

        ClearMatches(matches);
    }

    // Find matches on the board and return if found any
    private List<Vector2Int> MatchesFound()
    {
        // TODO: find matches logic
        return null;
    }

    // Clear the found matches
    private void ClearMatches(List<Vector2Int> matches)
    {
        for(int i = 0; i< matches.Count; i++)
        {
            _grid[matches[i].x, matches[i].y] = null;
        }

        ApplyGravity();
        Refill();
    }

    // Apply gravity to the cells
    private void ApplyGravity()
    {
        for (int x = 0; x < _width; x++)
        {
            int writeY = _height - 1; // bottom-most position we can write into

            // Scan from bottom to top
            for (int y = _height - 1; y >= 0; y--)
            {
                var piece = _grid[x, y];
                if (piece == null)
                    continue;

                if (y != writeY)
                {
                    _grid[x, writeY] = piece;
                    _grid[x, y] = null;
                }

                writeY--;
            }
        }
    }

    // Refill the empty cells
    private void Refill()
    {
        for (int x = 0; x < _width; x++) // Columns
        {
            for (int y = 0; y < _height; y++) // Rows
            {
                var cell = new Vector2Int(x, y);

                Animal chosen = _allowedAnimals[Random.Range(0, _allowedAnimals.Count)];

                if (_grid[x, y] == null)
                {
                    _grid[x, y] = chosen;
                }


            }
        }
    }

    // Get an animal from a cell
    public Animal GetAnimalFromCell(Vector2Int cell)
    {
        if (IsCellInBounds(cell))
        {
            return _grid[cell.x, cell.y];
        }

        return null;
    }
    
    // Put an animal in a cell
    public void SetAnimalInCell(Vector2Int cell, Animal animal)
    {
        if (IsCellInBounds(cell))
        {
            _grid[cell.x, cell.y] = animal;
        }
    }
    
    // Check if the cell is in the grid
    private bool IsCellInBounds(Vector2Int cell)
    {
        if ((0 <= cell.x && cell.x < _width) && (0 <= cell.y && cell.y < _height))
        {
            return true;
        }

        return false;
    }

    // Check if the cells are neighbours
    private bool AreCellsNeighbours(Vector2Int cell1, Vector2Int cell2)
    {
        var dx = Math.Abs(cell1.x - cell2.x);
        var dy = Math.Abs(cell1.y - cell2.y);

        if (dx + dy == 1)
        {
            return true;
        }

        return false;
    }

    // Checks if there'll be intial matches (Before the player starts playing himself)
    private bool WouldCreateInitialMatches(Vector2Int cell, Animal candidateAnimal)
    {
        // Horizontal check
        if (cell.x >= 2)
        {
            if (_grid[cell.x - 1, cell.y] == candidateAnimal &&
                _grid[cell.x - 2, cell.y] == candidateAnimal)
            {
                return true;
            }
        }

        // Vertical check
        if (cell.y >= 2)
        {
            if (_grid[cell.x, cell.y - 1] == candidateAnimal &&
                _grid[cell.x, cell.y - 2] == candidateAnimal)
            {
                return true;
            }
        }

        return false;
    }
}
