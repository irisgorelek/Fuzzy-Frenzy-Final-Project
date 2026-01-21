using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Random = UnityEngine.Random;

public class Board
{
    private List<Animal> _allowedAnimals; // Which animals are allowed on the board
    private Animal[,] _grid;
    private int _width;
    private int _height;
    private int _points = 0;
    private int _matchedAnimals = 0;

    public int CurrentPoints => _points;
    public int AnimalsCount => _matchedAnimals;

    public int _pointGoal { get; private set; }
    public int _matchedAnimalsGoal { get; private set; }

    public Board(BoardConfig config)
    {
        _width = config.weidth;
        _height = config.height;
        _pointGoal = config.pointGoal;
        _matchedAnimalsGoal = config.matchedAnimals;

        // Get the allowed animals for the level
        _allowedAnimals = new List<Animal>(config.animals);

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
    public bool TrySwapCells(Vector2Int cell1, Vector2Int cell2)
    {
        bool didSwap = false;

        // Swap the cells and check what happens
        if((IsCellInBounds(cell1) && IsCellInBounds(cell2)) && AreCellsNeighbours(cell1, cell2))
        {
            Animal temp = _grid[cell1.x, cell1.y];
            _grid[cell1.x, cell1.y] = _grid[cell2.x, cell2.y];
            _grid[cell2.x, cell2.y] = temp;

            List<Vector2Int> matches = MatchesFound();

            // If there were no matches found return the cells back to what they were
            if (matches.Count == 0)
            {
                _grid[cell2.x, cell2.y] = _grid[cell1.x, cell1.y];
                _grid[cell1.x, cell1.y] = temp;
                return didSwap;
            }

            didSwap = true;

            while (matches.Count > 0)
            {
                ClearMatches(matches);
                matches = MatchesFound();
            }
        }

        return didSwap;
    }

    // Find matches on the board and return a list of matches found
    private List<Vector2Int> MatchesFound()
    {
        HashSet<Vector2Int> matchedCells = new HashSet<Vector2Int>(); // Prevent coordinates duplicates
        List<Vector2Int> matchesList = new List<Vector2Int>();
        int sameAnimalCounter = 1;

        for (int x = 0; x < _width; x++)
        {
            sameAnimalCounter = 1;  

            for (int y = 1; y < _height; y++)
            {
                if (_grid[x, y] == null || _grid[x, y - 1] == null)
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

                if (_grid[x, y] == _grid[x, y - 1])
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
                int endY = _height - 1;
                for (int i = 0; i < sameAnimalCounter; i++)
                {
                    matchedCells.Add(new Vector2Int(x, endY - i));
                }
            }
        }

        for (int y = 0; y < _height; y++)
        {
            sameAnimalCounter = 1;

            for (int x = 1; x < _width; x++)
            {
                // Break on nulls (and flush any run that ended at y-1)
                if (_grid[x, y] == null || _grid[x - 1, y] == null)
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

                if (_grid[x - 1, y] == _grid[x, y])
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
                int endX = _width - 1;
                for (int i = 0; i < sameAnimalCounter; i++)
                {
                    matchedCells.Add(new Vector2Int(endX - i, y));
                }
            }
        }

        return matchedCells.ToList();
    }

    // Clear the found matches
    private void ClearMatches(List<Vector2Int> matches)
    {
        for(int i = 0; i< matches.Count; i++)
        {
            // Count the amount of points added and the amount of animals matched
            Debug.LogWarning($"Clearing: {_grid[matches[i].x, matches[i].y]}");
            _points += _grid[matches[i].x, matches[i].y]._points;
            _matchedAnimals++;

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
                if (_grid[x, y] != null)
                    continue;

                _grid[x, y] = _allowedAnimals[Random.Range(0, _allowedAnimals.Count)]; // If the cell is empty, add a random animal
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

    public List<Vector2Int> FindMatches()
    {
        return MatchesFound();
    }

    public void ResolveMatches(List<Vector2Int> matches)
    {
        ClearMatches(matches);
    }

    public bool SwapCellsRaw(Vector2Int cell1, Vector2Int cell2)
    {
        if (!(IsCellInBounds(cell1) && IsCellInBounds(cell2))) return false;
        if (!AreCellsNeighbours(cell1, cell2)) return false;

        // Swapping the values with a tupple
        (_grid[cell2.x, cell2.y], _grid[cell1.x, cell1.y]) = (_grid[cell1.x, cell1.y], _grid[cell2.x, cell2.y]);
        return true;
    }
    public bool HasAnyMatch()
    {
        return MatchesFound().Count > 0;
    }
}
