using System;
using System.Collections.Generic;
using FuzzyFrenzy.Domain;
using UnityEngine;

/// <summary>
/// Unity-facing adapter over the core board (<see cref="BoardState"/>). It speaks in Animal assets and
/// Vector2Int, and owns the level's core objects: the board, the session and the tile catalog.
/// All changes to the board during play go through <see cref="Match3Game"/>.
/// </summary>
public class Board : IAnimalGrid
{
    /// <summary>A tile falling from one cell to another, as the view animates it.</summary>
    public struct FallMove
    {
        public Vector2Int from;
        public Vector2Int to;
    }

    /// <summary>A new tile entering the board, as the view animates it.</summary>
    public struct SpawnInfo
    {
        public Vector2Int cell;   // where it ends
        public Animal animal;     // what spawned
        public int spawnFromY;    // -1 = above board, otherwise y of the blocker above
    }

    private readonly AnimalCatalog _catalog = new AnimalCatalog();
    private readonly BoardState _state;
    private readonly GameSession _session;
    private readonly IRandomSource _random;

    public Board(BoardConfig config) : this(config, new UnityRandomSource()) { }

    public Board(BoardConfig config, IRandomSource random)
    {
        if (config == null) throw new ArgumentNullException(nameof(config));

        _random = random ?? new UnityRandomSource();
        _state = new BoardState(_catalog.Tiles, config.weidth, config.height);

        if (config.animals != null)
            foreach (var animal in config.animals)
                if (animal != null) _state.SpawnPool.Add(_catalog.TypeOf(animal));

        _state.Specials.Wolf = _catalog.TypeOf(config.wolf);
        _state.Specials.Sheep = _catalog.TypeOf(config.sheep);
        _state.Specials.Bone = _catalog.TypeOf(config.boneBlock);
        _state.Specials.BlackSheep = _catalog.TypeOf(config.blackSheep);

        _session = new GameSession(BuildGoals(config));
    }

    // ---- Core objects ----

    public BoardState State => _state;
    public GameSession Session => _session;
    public AnimalCatalog Catalog => _catalog;
    public IRandomSource Random => _random;

    // ---- Read-only state ----

    public int Width => _state.Width;
    public int Height => _state.Height;
    public int CurrentPoints => _session.Points;
    public int GoalAmount => _session.Goals.Amount;
    public PointsOrMatches GoalType => (PointsOrMatches)(int)_session.Goals.Primary;

    public Animal GetAnimalFromCell(Vector2Int cell) => _catalog.AnimalOf(_state.TypeAt(cell.ToGrid()));

    public bool IsCellInBounds(Vector2Int cell) => _state.InBounds(cell.ToGrid());

    public List<Vector2Int> FindCellsWithAnimal(Animal animal)
    {
        var result = new List<Vector2Int>();
        if (animal == null) return result;

        int type = _catalog.TypeOf(animal);
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (_state.TypeAt(new GridPos(x, y)) == type)
                    result.Add(new Vector2Int(x, y));

        return result;
    }

    public List<Vector2Int> GetAllCells()
    {
        var cells = new List<Vector2Int>(Width * Height);
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                cells.Add(new Vector2Int(x, y));
        return cells;
    }

    // ---- Setup ----

    /// <summary>Fills the board with no starting matches and no sheep next to a wolf.</summary>
    public void Initialize() => BoardGenerator.Generate(_state, _random);

    /// <summary>Places a tile directly. For level setup, debug tools and tests, not for gameplay.</summary>
    public void SetAnimalInCell(Vector2Int cell, Animal animal)
    {
        if (IsCellInBounds(cell))
            _state.Place(cell.ToGrid(), _catalog.TypeOf(animal));
    }

    /// <summary>Debug only: lets a level without these tiles use them.</summary>
    public void DebugSetSpecialAnimals(Animal blackSheep, Animal boneBlock)
    {
        _state.Specials.BlackSheep = _catalog.TypeOf(blackSheep);
        _state.Specials.Bone = _catalog.TypeOf(boneBlock);
    }

    // Kept under this name: tests read it through reflection.
    private List<Vector2Int> MatchesFound()
    {
        var cells = new List<GridPos>();
        MatchFinder.Find(_state, cells);
        return cells.ToVectors();
    }

    private static LevelGoals BuildGoals(BoardConfig config)
    {
        var collect = new List<CollectGoal>();
        if (config.collectGoals != null)
            foreach (var goal in config.collectGoals)
                if (goal != null && goal.animal != null)
                    collect.Add(new CollectGoal(goal.animal._id, goal.amount));

        return new LevelGoals((GoalKind)(int)config.goalType, config.goal, collect);
    }
}
