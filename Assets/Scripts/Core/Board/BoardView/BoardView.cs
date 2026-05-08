using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BoardView : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private RectTransform _boardParent;
    [SerializeField] private GridLayoutGroup gridLayout;
    public bool SwapsEnabled = true;

    [Header("Prefabs")]
    [SerializeField] private GameObject _cell;

    [Header("Highlighting")] // For highlighting
    [SerializeField] Color _selectedColor;
    [SerializeField] Color _normalColor;
    [SerializeField] Color _tutorialLockedColor = new Color(1f, 0.9f, 0.35f, 1f);

    [Header("Animation")]
    [SerializeField] private RectTransform _swapOverlay;

    [Header("Art")]
    [SerializeField] private Sprite _defaultSprite;  // For null animals
    [SerializeField] private Image _backgroundImage;

    [Header("LevelNumber")]
    [SerializeField] private TextMeshProUGUI _levelNumberText;

    [Header("Goal")]
    [SerializeField] private Transform _goalRowsParent;
    [SerializeField] private GoalRowView _animalGoalRowPrefab;
    [SerializeField] private GoalRowView _primaryGoalRowPrefab;

    [Header("Moves")]
    [SerializeField] private TextMeshProUGUI _movesCountText;

    [Header("Timer")]
    [SerializeField] private TextMeshProUGUI _timerPowerUp;
    [SerializeField] private Image _timerBackground;

    [Header("Match FX")]
    [SerializeField] private Sprite _matchRingSprite;     // Thin white circle/ring sprite
    [SerializeField] private Sprite _sparkleSprite;       // Tiny star / diamond / soft dot
    [SerializeField] private Color _matchFxColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private int _sparklesPerMatch = 4; // For the PC 4 looks great

    [Header("ShuffleBoard")]
    //[SerializeField] private TextMeshProUGUI _shuffleMessage;
    [SerializeField] private Image _shufflePopUp;

    [Header("Bomb FX")]
    [SerializeField] private Sprite _bombRingSprite;
    [SerializeField] private Color _bombRingColor = new Color(1f, 1f, 1f, 0.95f);

    private readonly Dictionary<Vector2Int, CellView> _cells = new();

    public int _width { get; set; }
    public int _height { get; set; }

    private BoardViewGrid _grid;
    private BoardViewImagePool _imagePool;
    private BoardViewGoalRowsPresenter _goalRowsPresenter;
    private BoardViewHud _hud;
    private BoardViewInputHandler _input;
    private BoardViewSwapAnimator _swapAnimator;
    private BoardViewGravityAnimator _gravityAnimator;
    private BoardViewMatchFxAnimator _matchFxAnimator;
    private BoardViewShuffleAnimator _shuffleAnimator;
    private BoardViewBombAnimator _bombAnimator;

    private BoardViewGrid Grid => _grid ??= new BoardViewGrid(_cells, _defaultSprite);
    private BoardViewImagePool ImagePool => _imagePool ??= new BoardViewImagePool(_swapOverlay != null ? _swapOverlay : transform as RectTransform);
    private BoardViewGoalRowsPresenter GoalRowsPresenter =>
        _goalRowsPresenter ??= new BoardViewGoalRowsPresenter(_goalRowsParent, _animalGoalRowPrefab, _primaryGoalRowPrefab);
    private BoardViewHud Hud => _hud ??= new BoardViewHud(_levelNumberText, _backgroundImage, _movesCountText, _timerPowerUp, _timerBackground, GoalRowsPresenter);
    private BoardViewSwapAnimator SwapAnimator => _swapAnimator ??= new BoardViewSwapAnimator(Grid, ImagePool);
    private BoardViewGravityAnimator GravityAnimator => _gravityAnimator ??= new BoardViewGravityAnimator(Grid, ImagePool);
    private BoardViewMatchFxAnimator MatchFxAnimator => _matchFxAnimator ??= new BoardViewMatchFxAnimator(Grid, ImagePool, _matchRingSprite, _sparkleSprite, _matchFxColor, _sparklesPerMatch);
    private BoardViewShuffleAnimator ShuffleAnimator => _shuffleAnimator ??= new BoardViewShuffleAnimator(Grid, _shufflePopUp);
    private BoardViewBombAnimator BombAnimator => _bombAnimator ??= new BoardViewBombAnimator(Grid, ImagePool, _bombRingSprite, _matchRingSprite, _bombRingColor);
    private BoardViewInputHandler Input => _input ??= new BoardViewInputHandler(
        Grid,
        () => SwapsEnabled,
        (a, b, dir, duration) => AnimateInvalidSwap(a, b, dir, duration),
        (cell, duration) => AnimateBlockedTap(cell, duration)
    );

    public event Action<Vector2Int, Vector2Int> SwapRequested
    {
        add => Input.SwapRequested += value;
        remove => Input.SwapRequested -= value;
    }

    public event Action<Vector2Int> CellTapped
    {
        add => Input.CellTapped += value;
        remove => Input.CellTapped -= value;
    }

    public Func<Vector2Int, bool> CanStartSwap
    {
        get => Input.CanStartSwap;
        set => Input.CanStartSwap = value;
    }

    //public void ShowMoves(bool show) => _movesCountText.gameObject.SetActive(show);

    public void Build(int width, int height)
    {
        _width = width;
        _height = height;

        Grid.Build(
            width,
            height,
            _boardParent,
            gridLayout,
            _cell,
            _selectedColor,
            _normalColor,
            _tutorialLockedColor,
            Input.OnCellPointerDown,
            Input.OnCellDrag,
            Input.OnCellPointerUp
        );
    }

    public void AssignSprites(Board board) => Grid.AssignSprites(board);
    public void RefreshCellSprite(Vector2Int coord, Board board) => Grid.RefreshCellSprite(coord, board);
    public void SwapCellVisuals(Vector2Int a, Vector2Int b) => Grid.SwapCellVisuals(a, b);

    public void SetLevelNumber(int level) => Hud.SetLevelNumber(level);
    public void SetBackground(Sprite backgroundSprite) => Hud.SetBackground(backgroundSprite);
    public void ShowGoal(bool show) => Hud.ShowGoal(show);
    public void SetScore(int points, int totalPoints) => Hud.SetScore(points, totalPoints);
    public void SetMatchedAnimals(int animals, int goal) => Hud.SetMatchedAnimals(animals, goal);
    public void SetCollectGoals(List<AnimalGoal> goals, Dictionary<string, int> collected) => Hud.SetCollectGoals(goals, collected);
    public void SetPointsAndCollectGoals(int points, int pointsGoal, List<AnimalGoal> goals, Dictionary<string, int> collected) => Hud.SetPointsAndCollectGoals(points, pointsGoal, goals, collected);
    public void SetMatchesAndCollectGoals(int matched, int matchGoal, List<AnimalGoal> goals, Dictionary<string, int> collected) => Hud.SetMatchesAndCollectGoals(matched, matchGoal, goals, collected);
    public void SetMovesText(int movesLeft) => Hud.SetMovesText(movesLeft);
    public void SetMovesLastMoveTension(bool active) => Hud.SetMovesLastMoveTension(active);
    public void SetTimerVisible(bool visible) => Hud.SetTimerVisible(visible);
    public void SetTimerSeconds(int seconds) => Hud.SetTimerSeconds(seconds);

    public Task AnimateSwap(Vector2Int a, Vector2Int b, float duration = 0.18f) => SwapAnimator.AnimateSwap(a, b, duration);
    public Task AnimateWolfNudge(Vector2Int wolfCell, Vector2Int sheepCell, float duration = 0.10f) => SwapAnimator.AnimateWolfNudge(wolfCell, sheepCell, duration);
    public Task AnimateInvalidSwap(Vector2Int a, Vector2Int? b = null, Vector2Int? dir = null, float duration = 0.20f) => SwapAnimator.AnimateInvalidSwap(a, b, dir, duration);
    public Task AnimateBlockedTap(Vector2Int cell, float duration = 0.12f) => SwapAnimator.AnimateBlockedTap(cell, duration);

    public Task AnimateGravity(List<Board.FallMove> moves, List<Board.SpawnInfo> spawns, Board board, float duration = 0.20f) => GravityAnimator.AnimateGravity(moves, spawns, board, duration);
    public Task AnimateHint(Vector2Int a, Vector2Int b, float duration = 0.2f) => MatchFxAnimator.AnimateHint(a, b, duration);
    public Task AnimateMatchPopFx(List<Vector2Int> matches, float duration = 0.12f) => MatchFxAnimator.AnimateMatchPopFx(matches, duration);
    public Task ShowShuffleMessage(float hold = 1.2f) => ShuffleAnimator.ShowShuffleMessage(hold);
    public Task AnimateShuffle(Board board, float outDuration = 0.08f, float inDuration = 0.1f, float stagger = 0.002f) => ShuffleAnimator.AnimateShuffle(board, outDuration, inDuration, stagger);
    public Task AnimateBombImpact(List<Vector2Int> affected, float duration = 0.28f) => BombAnimator.AnimateBombImpact(affected, duration);
    public Task AnimateBombWarning(Vector2Int coord, float totalDuration = 1.5f) => BombAnimator.AnimateBombWarning(coord, totalDuration);

    public Vector3 GetCellWorldPosition(Vector2Int coord) => Grid.GetCellWorldPosition(coord);
    public void SetTutorialLockedCell(Vector2Int? coord) => Grid.SetTutorialLockedCell(coord);
    public Vector3 GetCellScenePosition(Vector2Int coord, Camera worldCamera, float worldZ = 0f) => Grid.GetCellScenePosition(coord, worldCamera, worldZ);

    public Transform GetFxParent()
    {
        return _swapOverlay != null ? _swapOverlay : transform;
    }
}
