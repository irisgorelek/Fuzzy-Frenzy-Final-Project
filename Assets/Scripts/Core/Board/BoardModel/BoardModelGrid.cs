using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class BoardModelGrid
{
    private readonly Board _board;

    public BoardModelGrid(Board board)
    {
        _board = board;
    }

    // Get an animal from a cell
    public Animal GetAnimalFromCell(Vector2Int cell)
    {
        if (IsCellInBounds(cell))
        {
            return _board._grid[cell.x, cell.y];
        }

        return null;
    }

    // Put an animal in a cell
    public void SetAnimalInCell(Vector2Int cell, Animal animal)
    {
        if (IsCellInBounds(cell))
        {
            _board._grid[cell.x, cell.y] = animal;
        }
    }

    // Check if the cell is in the grid
    public bool IsCellInBounds(Vector2Int cell)
    {
        if ((0 <= cell.x && cell.x < _board._width) && (0 <= cell.y && cell.y < _board._height))
        {
            return true;
        }

        return false;
    }

    // Check if the cells are neighbours
    public bool AreCellsNeighbours(Vector2Int cell1, Vector2Int cell2)
    {
        var dx = Math.Abs(cell1.x - cell2.x);
        var dy = Math.Abs(cell1.y - cell2.y);

        if (dx + dy == 1)
        {
            return true;
        }

        return false;
    }

    public bool IsMatchable(Animal animal)
    {
        return animal != null && animal._canMatch;
    }

    public Animal PickRandomAllowedAnimal()
    {
        return BoardModelRandomAnimalPicker.PickRandomAllowedAnimal(_board._allowedAnimals);
    }

    public List<Vector2Int> FindCellsWithAnimal(Animal animal)
    {
        var result = new List<Vector2Int>();
        if (animal == null)
            return result;

        for (int x = 0; x < _board._width; x++)
        {
            for (int y = 0; y < _board._height; y++)
            {
                if (_board._grid[x, y] == animal)
                    result.Add(new Vector2Int(x, y));
            }
        }

        return result;
    }

    // Helper method
    public List<Vector2Int> GetAllCells()
    {
        var cells = new List<Vector2Int>();

        for (int x = 0; x < _board.Width; x++)
        {
            for (int y = 0; y < _board.Height; y++)
            {
                cells.Add(new Vector2Int(x, y));
            }
        }

        return cells;
    }
}
