using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Board
{
    public struct FallMove
    {
        public Vector2Int from;
        public Vector2Int to;
    }

    public struct SpawnInfo
    {
        public Vector2Int cell;   // where it ends
        public Animal animal;     // what spawned
        public int spawnFromY;    // -1 = above board, otherwise y of the blocker above
    }

    private List<Animal> _allowedAnimals; // Which animals are allowed on the board
    private Animal[,] _grid;
    private int _width;
    private int _height;

    private int _points = 0;
    private int _matchedAnimals = 0;

    private int _goalAmount = 0;
    private PointsOrMatches _goalType;

    // Special Pieces
    private readonly Animal _wolf;
    private readonly Animal _sheep;
    private readonly Animal _boneBlock;
    private readonly Animal _blackSheep;
    private bool _blackSheepArmed; // when true, spawn one black sheep during next refill

    private static readonly Vector2Int[] OrthogonalDirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public int CurrentPoints => _points;
    public int MatchedAnimals => _matchedAnimals;
    public int Width => _width;
    public int Height => _height;

    public int GoalAmount => _goalAmount;
    public PointsOrMatches GoalType => _goalType;

    public bool IsGoalReached =>
        _goalType == PointsOrMatches.points
            ? _points >= _goalAmount
            : _matchedAnimals >= _goalAmount;

    public Action<string, int> OnAnimalsDestroyed;
    public Action<int> OnScoreAdded;

    public Board(BoardConfig config)
    {
        _width = config.weidth;
        _height = config.height;

        _goalAmount = config.goal;
        _goalType = config.goalType;

        // Get the allowed animals for the level
        _allowedAnimals = new List<Animal>(config.animals);
        _grid = new Animal[_width, _height];

        // Special Pieces
        _wolf = config.wolf;
        _sheep = config.sheep;
        _boneBlock = config.boneBlock;
        _blackSheep = config.blackSheep;
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
                    chosen = PickRandomAllowedAnimal(); // Create a board of random animals
                    attempts++;
                }
                while (WouldCreateInitialMatches(cell, chosen) && attempts < MaxPlacementAttempts);

                _grid[x, y] = chosen;
            }
        }

        if (_allowedAnimals.Contains(_boneBlock) && _boneBlock != null)
            _allowedAnimals.Remove(_boneBlock);

        if (_blackSheep != null)
            _allowedAnimals.Remove(_blackSheep);

        FixStartWolfSheepAdjacency(); // Make sure sheep don't appear next to wolves at the start of the level
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
                if (!IsMatchable(_grid[x, y]) || !IsMatchable(_grid[x, y - 1]))
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
                if (!IsMatchable(_grid[x, y]) || !IsMatchable(_grid[x - 1, y]))
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
    private void ClearMatches(List<Vector2Int> matches, List<FallMove> fallMoves = null, List<SpawnInfo> spawns = null)
    {
        var destroyedByAnimal = new Dictionary<string, int>();
        int pointsGainedThisClear = 0;

        for (int i = 0; i < matches.Count; i++)
        {
            var a = _grid[matches[i].x, matches[i].y];
            if (a == null) continue;

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFXPitchAdjusted(8, 0.2f); // Play pop sound.
            }

            _points += a._points;
            pointsGainedThisClear += a._points;
            _matchedAnimals++;

            string animalId = a._id;

            if (destroyedByAnimal.ContainsKey(animalId))
                destroyedByAnimal[animalId]++;
            else
                destroyedByAnimal[animalId] = 1;

            _grid[matches[i].x, matches[i].y] = null;
        }

        DamageAdjacentBoneBlocks(matches);

        if (pointsGainedThisClear > 0)
            OnScoreAdded?.Invoke(pointsGainedThisClear);

        foreach (var kvp in destroyedByAnimal)
        {
            OnAnimalsDestroyed?.Invoke(kvp.Key, kvp.Value);
        }

        ApplyGravity(fallMoves);
        Refill(spawns);

        ResolveWolfSheepInteractions(fallMoves, spawns);
    }

    // Apply gravity to the cells
    public void ApplyGravity(List<FallMove> fallMoves = null)
    {
        for (int x = 0; x < _width; x++)
        {
            int writeY = _height - 1; // Next slot we can write into

            // Scan from bottom to top
            for (int y = _height - 1; y >= 0; y--)
            {
                var piece = _grid[x, y];
                if (piece == null)
                    continue;

                // If bone block, don't apply gravity
                if (!piece._affectedByGravity)
                {
                    writeY = y - 1;
                    continue;
                }

                if (y != writeY)
                {
                    // Skip over obstacles if writeY is (somehow) pointing at one
                    while (writeY >= 0 && _grid[x, writeY] != null && !_grid[x, writeY]._affectedByGravity)
                        writeY--;

                    if (writeY < 0)
                        break;

                    fallMoves?.Add(new FallMove
                    {
                        from = new Vector2Int(x, y),
                        to = new Vector2Int(x, writeY)
                    });

                    _grid[x, writeY] = piece;
                    _grid[x, y] = null;
                }

                writeY--;
            }
        }
    }

    // Refill the empty cells
    public void Refill(List<SpawnInfo> spawns = null)
    {
        // Collect empty cells
        var empties = new List<Vector2Int>();
        for (int x = 0; x < _width; x++)
            for (int y = 0; y < _height; y++)
                if (_grid[x, y] == null)
                    empties.Add(new Vector2Int(x, y));

        // If armed, spawn exactly ONE black sheep into an empty spot
        if (_blackSheepArmed && _blackSheep != null && empties.Count > 0)
        {
            var chosenCell = empties[Random.Range(0, empties.Count)];

            _grid[chosenCell.x, chosenCell.y] = _blackSheep;

            spawns?.Add(new SpawnInfo
            {
                cell = chosenCell,
                animal = _blackSheep,
                spawnFromY = GetSpawnFromY(chosenCell)
            });

            _blackSheepArmed = false;
            empties.Remove(chosenCell);
        }

        // Fill remaining empties with normal animals
        foreach (var cell in empties)
        {
            var spawned = PickRandomAllowedAnimal();
            _grid[cell.x, cell.y] = spawned;

            spawns?.Add(new SpawnInfo
            {
                cell = cell,
                animal = spawned,
                spawnFromY = GetSpawnFromY(cell)
            });
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
    public bool IsCellInBounds(Vector2Int cell)
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

    public void ResolveMatches(List<Vector2Int> matches, List<FallMove> fallMoves, List<SpawnInfo> spawns)
    {
        ClearMatches(matches, fallMoves, spawns);
    }

    public bool SwapCellsRaw(Vector2Int cell1, Vector2Int cell2)
    {
        var a = _grid[cell1.x, cell1.y];
        var b = _grid[cell2.x, cell2.y];

        // Safety checks
        if ((a != null && !a._canSwap) || (b != null && !b._canSwap)) return false;
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

    public void ClearGridCell(Vector2Int cell)
    {
        if (_grid[cell.x, cell.y] == null) 
            return;

        _points += _grid[cell.x, cell.y]._points;
        _matchedAnimals++;
        _grid[cell.x, cell.y] = null;
    }

    private bool IsMatchable(Animal a) => a != null && a._canMatch;

    private Animal PickRandomAllowedAnimal()
    {
        // If there’s nothing to pick from, return null
        if (_allowedAnimals == null || _allowedAnimals.Count == 0)
            return null;

        // Compute the sum of all weights
        float total = 0f;
        for (int i = 0; i < _allowedAnimals.Count; i++)
            total += Mathf.Max(0f, _allowedAnimals[i]._spawnWeight);

        // If total is 0 (all weights were 0 or negative), fallback to uniform random
        if (total <= 0f)
            return _allowedAnimals[Random.Range(0, _allowedAnimals.Count)];

        // Pick a random number in [0, total)
        float r = Random.value * total;

        // Walk through the animals, adding weights until we “cross” r
        float cumulative = 0f;
        for (int i = 0; i < _allowedAnimals.Count; i++)
        {
            cumulative += Mathf.Max(0f, _allowedAnimals[i]._spawnWeight);

            // Select the first animal whose cumulative range contains r
            if (r <= cumulative)
                return _allowedAnimals[i];
        }

        return _allowedAnimals[_allowedAnimals.Count - 1];
    }

    // ----- Wolf -> Sheep interaction ----- //
    private void DamageAdjacentBoneBlocks(List<Vector2Int> matches)
    {
        if (_boneBlock == null || matches == null || matches.Count == 0)
            return;

        var toRemove = new HashSet<Vector2Int>();

        for (int i = 0; i < matches.Count; i++)
        {
            var m = matches[i];
            for (int d = 0; d < OrthogonalDirs.Length; d++)
            {
                var n = m + OrthogonalDirs[d];
                if (!IsCellInBounds(n)) continue;

                if (_grid[n.x, n.y] == _boneBlock)
                    toRemove.Add(n);
            }
        }

        foreach (var cell in toRemove)
            _grid[cell.x, cell.y] = null;
    }

    private void ResolveWolfSheepInteractions(List<FallMove> fallMoves = null, List<SpawnInfo> spawns = null)
    {
        if (_wolf == null || _sheep == null || _boneBlock == null)
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
                    OnScoreAdded?.Invoke(pointsGained); // Add the sheep in the score

                if (eatenCount > 0)
                    OnAnimalsDestroyed?.Invoke(_sheep._id, eatenCount); // Add the sheep as a match

                ApplyGravity(fallMoves);
                Refill(spawns);
            }
        }
        while (changed && safety++ < 100);
    }

    private bool ResolveWolfSheepOnce(out int eatenCount, out int pointsGained)
    {
        eatenCount = 0;
        pointsGained = 0;

        var sheepToEat = new HashSet<Vector2Int>();

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                if (_grid[x, y] != _sheep) continue;

                var cell = new Vector2Int(x, y);
                for (int d = 0; d < OrthogonalDirs.Length; d++)
                {
                    var n = cell + OrthogonalDirs[d];
                    if (!IsCellInBounds(n)) continue;

                    if (_grid[n.x, n.y] == _wolf)
                    {
                        sheepToEat.Add(cell);
                        break;
                    }
                }
            }
        }

        if (sheepToEat.Count == 0)
            return false;

        foreach (var cell in sheepToEat)
        {
            Debug.Log($"Wolf ate sheep at {cell} -> bone");
            var a = _grid[cell.x, cell.y];
            if (a == null) continue;

            eatenCount++;
            _points += a._points; // Add the points of the sheep eaten
            pointsGained += a._points;
            _matchedAnimals++; // Add the sheep as 1 match 

            // Sheep turns into a bone block
            _grid[cell.x, cell.y] = _boneBlock;
        }

        return true;
    }

    private void FixStartWolfSheepAdjacency()
    {
        if (_wolf == null || _sheep == null) return;

        const int MaxPasses = 50;
        const int MaxAttemptsPerCell = 30;

        for (int pass = 0; pass < MaxPasses; pass++)
        {
            bool changed = false;

            for (int x = 0; x < _width; x++)
                for (int y = 0; y < _height; y++)
                {
                    if (_grid[x, y] != _sheep) continue;

                    var cell = new Vector2Int(x, y);
                    if (!HasNeighbor(cell, _wolf)) continue;

                    // Reroll this sheep into something else that:
                    // isn't sheep or wolf, doesn't create a 3-match immediately, isn't adjacent to a wolf
                    for (int attempt = 0; attempt < MaxAttemptsPerCell; attempt++)
                    {
                        var candidate = PickRandomAllowedAnimal(); // your weighted picker (or your old random)

                        if (candidate == _sheep || candidate == _wolf) continue;
                        if (WouldCreateMatchAnywhere(cell, candidate)) continue;

                        _grid[cell.x, cell.y] = candidate;

                        if (HasNeighbor(cell, _wolf))
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
        for (int i = 0; i < OrthogonalDirs.Length; i++)
        {
            var n = cell + OrthogonalDirs[i];
            if (!IsCellInBounds(n)) continue;
            if (_grid[n.x, n.y] == target) return true;
        }
        return false;
    }

    private bool WouldCreateMatchAnywhere(Vector2Int cell, Animal candidate)
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
        while (IsCellInBounds(p) && _grid[p.x, p.y] == a)
        {
            c++;
            p += dir;
        }
        return c;
    }

    // ----- Black Sheep ----- //
    public void RollForBlackSheep(float chance01)
    {
        if (_blackSheep == null) return;
        if (_blackSheepArmed) return; // already queued

        chance01 = Mathf.Clamp01(chance01);
        if (Random.value < chance01)
            _blackSheepArmed = true;
    }

    public void TriggerSheepSwipeBlast(Vector2Int sheepPosAfterSwap, bool swipedVertically)
    {
        if (!IsCellInBounds(sheepPosAfterSwap))
            return;

        // vertical swipe -> ROW
        // horizontal swipe -> COLUMN
        var cellsToClear = new List<Vector2Int>();

        if (swipedVertically)
        {
            int y = sheepPosAfterSwap.y;
            for (int x = 0; x < _width; x++)
                cellsToClear.Add(new Vector2Int(x, y));
        }
        else
        {
            int x = sheepPosAfterSwap.x;
            for (int y = 0; y < _height; y++)
                cellsToClear.Add(new Vector2Int(x, y));
        }

        ClearCellsAsExplosion(cellsToClear);
    }

    private void ClearCellsAsExplosion(List<Vector2Int> cells)
    {
        var destroyedByAnimal = new Dictionary<string, int>();
        int pointsGainedThisClear = 0;

        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];
            var a = _grid[c.x, c.y];
            if (a == null) continue;

            // Dont clear bone blocks
            if (_boneBlock != null && a == _boneBlock)
                continue;

            _points += a._points;
            pointsGainedThisClear += a._points;
            _matchedAnimals++;

            string animalId = a._id;
            if (destroyedByAnimal.ContainsKey(animalId)) destroyedByAnimal[animalId]++;
            else destroyedByAnimal[animalId] = 1;

            _grid[c.x, c.y] = null;
        }

        // destroys bones adjacent to the explosion - Can delete if we don't want that
        DamageAdjacentBoneBlocks(cells);

        if (pointsGainedThisClear > 0)
            OnScoreAdded?.Invoke(pointsGainedThisClear);

        foreach (var kvp in destroyedByAnimal)
            OnAnimalsDestroyed?.Invoke(kvp.Key, kvp.Value);

        ApplyGravity();
        Refill();
        ResolveWolfSheepInteractions();
    }

    // Helper for gravaity + bone animation
    private int GetSpawnFromY(Vector2Int target)
    {
        // Search upward for the nearest blocker
        for (int y = target.y - 1; y >= 0; y--)
        {
            var above = _grid[target.x, y];
            if (above != null && !above._affectedByGravity)
                return y; // blocker location
        }
        return -1; // no blocker above -> spawn from above board
    }
}
