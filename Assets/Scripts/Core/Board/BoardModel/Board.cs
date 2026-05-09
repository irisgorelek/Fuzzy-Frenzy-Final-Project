using System;
using System.Collections.Generic;
using UnityEngine;

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

    internal List<Animal> _allowedAnimals; // Which animals are allowed on the board
    internal Animal[,] _grid;
    internal int _width;
    internal int _height;

    internal int _points = 0;
    internal int _matchedAnimals = 0;

    internal int _goalAmount = 0;
    internal PointsOrMatches _goalType;
    internal readonly HashSet<Vector2Int> _lockedCells = new HashSet<Vector2Int>();

    // Special Pieces
    internal readonly Animal _wolf;
    internal readonly Animal _sheep;
    internal readonly Animal _boneBlock;
    internal readonly Animal _blackSheep;
    internal bool _blackSheepArmed; // when true, spawn one black sheep during next refill

    internal static readonly Vector2Int[] OrthogonalDirs =
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
    public Action<Vector2Int, Vector2Int> OnWolfAteSheep;

    internal readonly Queue<Animal> _debugForcedSpawns = new Queue<Animal>(); // For the shuffle test

    private readonly BoardModelGrid _gridAccess;
    private readonly BoardModelSwapService _swapService;
    private readonly BoardModelScoreState _scoreState;
    private readonly BoardModelCellLockState _cellLockState;
    private readonly BoardModelInitializer _initializer;
    private readonly BoardModelDebugTools _debugTools;
    private readonly BoardModelMatchFinder _matchFinder;
    private readonly BoardModelCellClearer _cellClearer;
    private readonly BoardModelGravityResolver _gravityResolver;
    private readonly BoardModelSpecialPieces _specialPieces;
    private readonly BoardModelShuffler _shuffler;

    internal BoardModelMatchFinder MatchFinder => _matchFinder;
    internal BoardModelGravityResolver GravityResolver => _gravityResolver;
    internal BoardModelCellClearer CellClearer => _cellClearer;
    internal BoardModelSpecialPieces SpecialPieces => _specialPieces;

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

        _gridAccess = new BoardModelGrid(this);
        _swapService = new BoardModelSwapService(this);
        _scoreState = new BoardModelScoreState(this);
        _cellLockState = new BoardModelCellLockState(this);
        _matchFinder = new BoardModelMatchFinder(this);
        _gravityResolver = new BoardModelGravityResolver(this);
        _specialPieces = new BoardModelSpecialPieces(this);
        _cellClearer = new BoardModelCellClearer(this);
        _shuffler = new BoardModelShuffler(this);
        _initializer = new BoardModelInitializer(this);
        _debugTools = new BoardModelDebugTools(this);
    }

    public void Initialize() => _initializer.Initialize();

    // =================================== TEST ======================================== //
    public void InitializeStaticDeadBoard() => _debugTools.InitializeStaticDeadBoard();
    public void DebugSetForcedSpawns(params Animal[] animals) => _debugTools.DebugSetForcedSpawns(animals);
    // ============================== TEST =============================== //

    public List<Vector2Int> FindMatches() => _matchFinder.FindMatches();
    public bool HasAnyMatch() => _matchFinder.FindMatches().Count > 0;
    public void ResolveMatches(List<Vector2Int> matches, List<FallMove> fallMoves, List<SpawnInfo> spawns) => _cellClearer.ClearMatches(matches, fallMoves, spawns);
    public void ClearCells(IEnumerable<Vector2Int> cells, List<FallMove> fallMoves = null, List<SpawnInfo> spawns = null) => _cellClearer.ClearCells(cells, fallMoves, spawns);

    // Apply gravity to the cells
    public void ApplyGravity(List<FallMove> fallMoves = null) => _gravityResolver.ApplyGravity(fallMoves);

    // Refill the empty cells
    public void Refill(List<SpawnInfo> spawns = null) => _gravityResolver.Refill(spawns);

    // Get an animal from a cell
    public Animal GetAnimalFromCell(Vector2Int cell) => _gridAccess.GetAnimalFromCell(cell);

    // Put an animal in a cell
    public void SetAnimalInCell(Vector2Int cell, Animal animal) => _gridAccess.SetAnimalInCell(cell, animal);

    // Check if the cell is in the grid
    public bool IsCellInBounds(Vector2Int cell) => _gridAccess.IsCellInBounds(cell);

    // Check if the cells are neighbours
    internal bool AreCellsNeighbours(Vector2Int cell1, Vector2Int cell2) => _gridAccess.AreCellsNeighbours(cell1, cell2); // internal means here that only the specific assembley can use this function 

    public bool SwapCellsRaw(Vector2Int cell1, Vector2Int cell2) => _swapService.SwapCellsRaw(cell1, cell2);
    public void ClearGridCell(Vector2Int cell) => _scoreState.ClearGridCell(cell);
    internal bool IsMatchable(Animal animal) => _gridAccess.IsMatchable(animal);
    internal Animal PickRandomAllowedAnimal() => _gridAccess.PickRandomAllowedAnimal();

    public void ResolveWolfSheepAfterCascades() => _specialPieces.ResolveWolfSheepAfterCascades();

    // ----- Black Sheep ----- //
    public void RollForBlackSheep(float chance01) => _specialPieces.RollForBlackSheep(chance01);
    public void TriggerSheepSwipeBlast(Vector2Int sheepPosAfterSwap, bool swipedVertically) => _specialPieces.TriggerSheepSwipeBlast(sheepPosAfterSwap, swipedVertically);

    public void ShuffleSwappablePieces() => _shuffler.ShuffleSwappablePieces();
    public bool ShuffleUntilPlayable(BoardHintFinder hintFinder, int maxAttempts = 100) => _shuffler.ShuffleUntilPlayable(hintFinder, maxAttempts);

    public List<Vector2Int> FindCellsWithAnimal(Animal animal) => _gridAccess.FindCellsWithAnimal(animal);
    public List<Vector2Int> GetAllCells() => _gridAccess.GetAllCells();

    public void SetLockedCells(IEnumerable<Vector2Int> cells) => _cellLockState.SetLockedCells(cells);
    public void ClearLockedCells() => _cellLockState.ClearLockedCells();
    public bool IsCellLocked(Vector2Int cell) => _cellLockState.IsCellLocked(cell);

    public void DebugCheatFillPrimaryGoal() => _scoreState.DebugCheatFillPrimaryGoal();
}
