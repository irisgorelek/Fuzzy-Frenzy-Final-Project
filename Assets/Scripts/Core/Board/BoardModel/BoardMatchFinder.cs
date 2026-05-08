using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardModelMatchFinder
{
    private readonly Board _board;

    public BoardModelMatchFinder(Board board)
    {
        _board = board;
    }

    // Find matches on the board and return a list of matches found
    public List<Vector2Int> FindMatches()
    {
        HashSet<Vector2Int> matchedCells = new HashSet<Vector2Int>(); // Prevent coordinates duplicates
        int sameAnimalCounter = 1;

        for (int x = 0; x < _board._width; x++)
        {
            sameAnimalCounter = 1;

            for (int y = 1; y < _board._height; y++)
            {
                if (!_board.IsMatchable(_board._grid[x, y]) || !_board.IsMatchable(_board._grid[x, y - 1]))
                {
                    if (sameAnimalCounter >= 3)
                    {
                        int endY = y - 1;
                        for (int i = 0; i < sameAnimalCounter; i++)
                            matchedCells.Add(new Vector2Int(x, endY - i));
                    }
                    sameAnimalCounter = 1;
                    continue;
                }

                if (_board._grid[x, y] == _board._grid[x, y - 1])
                {
                    sameAnimalCounter++;
                }
                else
                {
                    if (sameAnimalCounter >= 3) // If the same animal appeared 3+ times in a row add the cells to the list
                    {
                        int endY = y - 1;
                        for (int i = 0; i < sameAnimalCounter; i++)
                        {
                            matchedCells.Add(new Vector2Int(x, endY - i));
                        }
                    }

                    sameAnimalCounter = 1;
                }
            }

            // Flush a run that continues to the bottom of the column
            if (sameAnimalCounter >= 3)
            {
                int endY = _board._height - 1;
                for (int i = 0; i < sameAnimalCounter; i++)
                {
                    matchedCells.Add(new Vector2Int(x, endY - i));
                }
            }
        }

        for (int y = 0; y < _board._height; y++)
        {
            sameAnimalCounter = 1;

            for (int x = 1; x < _board._width; x++)
            {
                // Break on nulls (and flush any run that ended at y-1)
                if (!_board.IsMatchable(_board._grid[x, y]) || !_board.IsMatchable(_board._grid[x - 1, y]))
                {
                    if (sameAnimalCounter >= 3)
                    {
                        int endX = x - 1;
                        for (int i = 0; i < sameAnimalCounter; i++)
                            matchedCells.Add(new Vector2Int(endX - i, y));
                    }
                    sameAnimalCounter = 1;
                    continue;
                }

                if (_board._grid[x - 1, y] == _board._grid[x, y])
                {
                    sameAnimalCounter++;
                }
                else // If the animal changed, add the matching cells to the list
                {
                    if (sameAnimalCounter >= 3) // If the same animal appeared 3+ times in a row add the cells to the list
                    {
                        int endX = x - 1;
                        for (int i = 0; i < sameAnimalCounter; i++)
                        {
                            matchedCells.Add(new Vector2Int(endX - i, y));
                        }
                    }

                    sameAnimalCounter = 1;
                }
            }

            // Flush a run that continues to the end of the row
            if (sameAnimalCounter >= 3)
            {
                int endX = _board._width - 1;
                for (int i = 0; i < sameAnimalCounter; i++)
                {
                    matchedCells.Add(new Vector2Int(endX - i, y));
                }
            }
        }

        return matchedCells.ToList();
    }

    // Checks if there'll be intial matches (Before the player starts playing himself)
    public bool WouldCreateInitialMatches(Vector2Int cell, Animal candidateAnimal)
    {
        // Horizontal check
        if (cell.x >= 2)
        {
            if (_board._grid[cell.x - 1, cell.y] == candidateAnimal &&
                _board._grid[cell.x - 2, cell.y] == candidateAnimal)
            {
                return true;
            }
        }

        // Vertical check
        if (cell.y >= 2)
        {
            if (_board._grid[cell.x, cell.y - 1] == candidateAnimal &&
                _board._grid[cell.x, cell.y - 2] == candidateAnimal)
            {
                return true;
            }
        }

        return false;
    }

    public bool WouldCreateMatchAnywhere(Vector2Int cell, Animal candidate)
    {
        int h = 1 + CountInDir(cell, Vector2Int.left, candidate) + CountInDir(cell, Vector2Int.right, candidate);
        if (h >= 3) return true;

        int v = 1 + CountInDir(cell, Vector2Int.down, candidate) + CountInDir(cell, Vector2Int.up, candidate);
        return v >= 3;
    }

    // Counts how many of the same animals are in a straight line starting from the cell next to start
    private int CountInDir(Vector2Int start, Vector2Int dir, Animal a)
    {
        int c = 0;
        var p = start + dir;
        while (_board.IsCellInBounds(p) && _board._grid[p.x, p.y] == a)
        {
            c++;
            p += dir;
        }
        return c;
    }
}
