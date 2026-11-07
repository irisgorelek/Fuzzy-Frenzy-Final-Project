using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The board's scene component. Holds the Inspector setup and the cell grid, and puts together the parts
/// that do the work: <see cref="BoardHud"/> (goals, moves, timer), <see cref="BoardTileAnimator"/> (moving
/// tiles), <see cref="BoardEffectsPlayer"/> (clear and booster effects) and <see cref="BoardSwipeInput"/>
/// (swipes and taps). Callers that only need one part depend on <see cref="IBoardAnimator"/> or
/// <see cref="IBoardHud"/>.
/// </summary>
public class BoardView : MonoBehaviour, IBoardAnimator, IBoardHud
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
    [Header("Tile landing")]
    [SerializeField, Range(0, .2f)] private float _landingBounceHeight = .08f;
    [SerializeField, Min(.02f)] private float _landingBounceSeconds = .14f;
    [Header("New tile entry")]
    [SerializeField, Min(.1f)] private float _spawnFallSeconds = .42f;
    [SerializeField, Min(.01f)] private float _spawnFadeSeconds = .30f;
    [SerializeField, Range(0, .15f)] private float _spawnStaggerSeconds = .055f;
    [SerializeField, Range(.2f, 1.2f)] private float _spawnEntryHeight = .9f;

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
    [SerializeField] private BoardMatchEffects _boardMatchEffects;
    [SerializeField] private BoardClearAnimation _clearAnimation;
    [SerializeField] private Sprite _matchRingSprite;     // Thin white circle/ring sprite
    [SerializeField] private Sprite _sparkleSprite;       // Tiny star / diamond / soft dot
    [SerializeField] private Color _matchFxColor = new Color(1f, 1f, 1f, 0.95f);
    [SerializeField] private int _sparklesPerMatch = 4; // For the PC 4 looks great

    [Header("ShuffleBoard")]
    [SerializeField] private Image _shufflePopUp;

    [Header("Bomb FX")]
    [SerializeField] private Sprite _bombRingSprite;
    [SerializeField] private Color _bombRingColor = new Color(1f, 1f, 1f, 0.95f);

    // Shared by every part. Tests and diagnostics read it through reflection under this name.
    private readonly Dictionary<Vector2Int, CellView> _cells = new Dictionary<Vector2Int, CellView>();

    // The parts are created on first use, so the view also works when added in edit mode without Awake.
    private TileImagePool _pool;
    private BoardHud _hud;
    private BoardTileAnimator _tiles;
    private BoardEffectsPlayer _effects;
    private BoardSwipeInput _input;
    private readonly TileMotionSettings _motion = new TileMotionSettings();
    private readonly BoardFxSettings _fx = new BoardFxSettings();

    public int _width { get; set; }
    public int _height { get; set; }

    public event Action<Vector2Int, Vector2Int> SwapRequested;
    public event Action<Vector2Int> CellTapped;

    public Func<Vector2Int, bool> CanStartSwap;

    int IBoardAnimator.Width => _width;
    int IBoardAnimator.Height => _height;

    private TileImagePool Pool => _pool ??= new TileImagePool(_swapOverlay);
    private BoardHud Hud => _hud ??= new BoardHud(new BoardHudParts
    {
        LevelNumberText = _levelNumberText,
        BackgroundImage = _backgroundImage,
        GoalRowsParent = _goalRowsParent,
        AnimalGoalRowPrefab = _animalGoalRowPrefab,
        PrimaryGoalRowPrefab = _primaryGoalRowPrefab,
        MovesCountText = _movesCountText,
        TimerText = _timerPowerUp,
        TimerBackground = _timerBackground,
    });
    private BoardTileAnimator Tiles => _tiles ??= new BoardTileAnimator(_cells, Pool, FillMotion(), _shufflePopUp, AssignSprites);
    private BoardEffectsPlayer Effects => _effects ??= new BoardEffectsPlayer(_cells, Pool, FillFx(), this);
    private BoardSwipeInput Swipes => _input ??= new BoardSwipeInput(this, _cells);

    private void OnValidate()
    {
        // Keep live tuning working in the editor.
        FillMotion();
        FillFx();
    }

    // ---------------- Grid ----------------

    public void Build(int width, int height)
    {
        _width = width;
        _height = height;

        // Change the grid layout according to the board config
        if (gridLayout == null || _boardParent == null || _cell == null || _defaultSprite == null)
        {
            Debug.LogError("Something in BoardView is null");
            return;
        }

        gridLayout.constraintCount = _width;

        // Clear old
        foreach (Transform child in _boardParent)
            Destroy(child.gameObject);

        _cells.Clear();

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                var coord = new Vector2Int(x, y);

                var go = Instantiate(_cell, _boardParent);
                var cellView = go.GetComponent<CellView>();
                cellView.Init(coord);
                cellView.ConfigureHighlight(_selectedColor, _normalColor);
                cellView.ConfigureTutorialLock(_tutorialLockedColor);
                Swipes.Attach(cellView);

                _cells.Add(coord, cellView);
            }
        }
    }

    /// <summary>Draws every cell from the board.</summary>
    public void AssignSprites(IAnimalGrid board)
    {
        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                var coord = new Vector2Int(x, y);
                var animal = board.GetAnimalFromCell(coord);

                if (animal == null)
                {
                    _cells[coord].SetSprite(_defaultSprite, Color.red);
                    continue;
                }

                _cells[coord].SetSprite(animal._sprite, animal.color);
                _cells[coord].SetImageEnabled(true);
            }
        }
    }

    /// <summary>Draws one animal in one cell, or the empty sprite for null.</summary>
    public void SetCellAnimal(Vector2Int coord, Animal animal)
    {
        if (!_cells.TryGetValue(coord, out var cellView))
            return;

        if (animal == null)
        {
            cellView.SetSprite(_defaultSprite, Color.red);
            return;
        }

        cellView.SetSprite(animal._sprite, animal.color);
    }

    public void SetTutorialLockedCell(Vector2Int? coord)
    {
        foreach (var kvp in _cells)
            kvp.Value.SetTutorialLocked(false);

        if (coord.HasValue && _cells.TryGetValue(coord.Value, out var cell))
            cell.SetTutorialLocked(true);
    }

    public Vector3 GetCellWorldPosition(Vector2Int coord) =>
        _cells.TryGetValue(coord, out var cell) ? cell.ImageRect.position : Vector3.zero;

    public Vector3 GetCellScenePosition(Vector2Int coord, Camera worldCamera, float worldZ = 0f)
    {
        if (!_cells.TryGetValue(coord, out var cell) || worldCamera == null)
            return Vector3.zero;

        Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(null, cell.ImageRect.position);

        float camDistance = Mathf.Abs(worldCamera.transform.position.z - worldZ);
        Vector3 worldPoint = worldCamera.ScreenToWorldPoint(new Vector3(screenPoint.x, screenPoint.y, camDistance));

        worldPoint.z = worldZ;
        return worldPoint;
    }

    public Transform GetFxParent() => _swapOverlay != null ? _swapOverlay : transform;

    public RectTransform GetMatchCellRect(Vector2Int cell) => _cells.TryGetValue(cell, out var view) ? view.ImageRect : null;

    // ---------------- Input (called by BoardSwipeInput) ----------------

    internal bool CanStartSwapAt(Vector2Int coord) => CanStartSwap == null || CanStartSwap(coord);
    internal void RaiseSwapRequested(Vector2Int from, Vector2Int to) => SwapRequested?.Invoke(from, to);
    internal void RaiseCellTapped(Vector2Int coord) => CellTapped?.Invoke(coord);

    // ---------------- HUD ----------------

    public void SetLevelNumber(int level) => Hud.SetLevelNumber(level);
    public void SetBackground(Sprite backgroundSprite) => Hud.SetBackground(backgroundSprite);
    public void ShowGoal(bool show) => Hud.ShowGoal(show);
    public void SetScore(int points, int totalPoints) => Hud.SetScore(points, totalPoints);
    public void SetMatchedAnimals(int animals, int goal) => Hud.SetMatchedAnimals(animals, goal);
    public void SetCollectGoals(List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected) => Hud.SetCollectGoals(goals, collected);
    public void SetPointsAndCollectGoals(int points, int pointsGoal, List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected) =>
        Hud.SetPointsAndCollectGoals(points, pointsGoal, goals, collected);
    public GoalRowView FindAnimalGoal(Sprite sprite) => Hud.FindAnimalGoal(sprite);
    public void SetMovesText(int movesLeft) => Hud.SetMovesText(movesLeft);
    public void SetMovesLastMoveTension(bool active) => Hud.SetMovesLastMoveTension(active);
    public void SetTimerVisible(bool visible) => Hud.SetTimerVisible(visible);
    public void SetTimerSeconds(int seconds) => Hud.SetTimerSeconds(seconds);

    // ---------------- Tile animation ----------------

    public Task AnimateSwap(Vector2Int a, Vector2Int b, float duration = 0.18f) => Tiles.AnimateSwap(a, b, duration);
    public Task AnimateGravity(List<Board.FallMove> moves, List<Board.SpawnInfo> spawns, IAnimalGrid board, float duration = 0.20f) =>
        Tiles.AnimateGravity(moves, spawns, board, duration);
    public Task AnimateWolfNudge(Vector2Int wolfCell, Vector2Int sheepCell, float duration = 0.10f) => Tiles.AnimateWolfNudge(wolfCell, sheepCell, duration);
    public Task AnimateHint(Vector2Int a, Vector2Int b, float duration = 0.2f) => Tiles.AnimateHint(a, b, duration);
    public Task ShowShuffleMessage(float hold = 1.2f) => Tiles.ShowShuffleMessage(hold);
    public Task AnimateShuffle(IAnimalGrid board, float outDuration = 0.08f, float inDuration = 0.1f, float stagger = 0.002f) =>
        Tiles.AnimateShuffle(board, outDuration, inDuration, stagger);
    public Task AnimateInvalidSwap(Vector2Int a, Vector2Int? b = null, Vector2Int? dir = null, float duration = 0.20f) =>
        Tiles.AnimateInvalidSwap(a, b, dir, duration);
    public Task AnimateBlockedTap(Vector2Int cell, float duration = 0.12f) => Tiles.AnimateBlockedTap(cell, duration);

    // ---------------- Effects ----------------

    public Task AnimateMatchPopFx(List<Vector2Int> matches, float duration = BoardEffectsTiming.MatchPop, BoardClearEffect effect = BoardClearEffect.Match) =>
        Effects.AnimateMatchPopFx(matches, duration, effect);
    public Task AnimateBombImpact(List<Vector2Int> affected, float duration = BoardEffectsTiming.BombImpact) => Effects.AnimateBombImpact(affected, duration);
    public Task AnimateBombWarning(Vector2Int coord, float totalDuration = 1f) => Effects.AnimateBombWarning(coord, totalDuration);
    public void PlayBoneBreakFx(Vector2Int coord) => Effects.PlayBoneBreakFx(coord);
    public Task AnimateSheepAnger(Vector2Int coord) => Effects.AnimateSheepAnger(coord);
    public Task WaitForBlackSheepParticles() => Effects.WaitForBlackSheepParticles();
    public void PlayBlackSheepFx(Vector2Int coord, bool horizontal, int width, int height) => Effects.PlayBlackSheepFx(coord, horizontal, width, height);
    public bool PlayBombBurstFx(Vector2Int coord) => Effects.PlayBombBurstFx(coord);

    // ---------------- Settings ----------------

    private TileMotionSettings FillMotion()
    {
        _motion.LandingBounceHeight = _landingBounceHeight;
        _motion.LandingBounceSeconds = _landingBounceSeconds;
        _motion.SpawnFallSeconds = _spawnFallSeconds;
        _motion.SpawnFadeSeconds = _spawnFadeSeconds;
        _motion.SpawnStaggerSeconds = _spawnStaggerSeconds;
        _motion.SpawnEntryHeight = _spawnEntryHeight;
        return _motion;
    }

    private BoardFxSettings FillFx()
    {
        _fx.Particles = _boardMatchEffects;
        _fx.ClearAnimation = _clearAnimation;
        _fx.MatchRingSprite = _matchRingSprite;
        _fx.SparkleSprite = _sparkleSprite;
        _fx.MatchFxColor = _matchFxColor;
        _fx.SparklesPerMatch = _sparklesPerMatch;
        _fx.BombRingSprite = _bombRingSprite;
        _fx.BombRingColor = _bombRingColor;
        return _fx;
    }
}
