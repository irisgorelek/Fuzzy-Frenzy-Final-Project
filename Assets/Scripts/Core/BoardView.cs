using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using System.Threading.Tasks;
using Random = UnityEngine.Random;

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
    [SerializeField] private TextMeshProUGUI _shuffleMessage;
    [SerializeField] private GameObject _shufflePopUp;

    [Header("Bomb FX")]
    [SerializeField] private Sprite _bombRingSprite;
    [SerializeField] private Color _bombRingColor = new Color(1f, 1f, 1f, 0.95f);

    private struct GoalRowSpec
    {
        public bool IsPrimary;
        public Sprite Icon;
        public string Text;
        public Color Color;
        public bool IsComplete;
    }

    private Dictionary<Vector2Int, CellView> _cells = new();
    public int _width {get; set;}
    public int _height {get; set;}


    public event Action<Vector2Int, Vector2Int> SwapRequested;
    public event Action<Vector2Int> CellTapped;

    // Swipe state
    private bool _gestureActive;
    private bool _swipeCommitted;
    private Vector2Int _startCell;
    private Vector2 _startScreenPos;
    private Vector2Int? _highlightedCell;
    private const float SwipeThresholdPixels = 45f;

    private readonly List<GoalRowView> _rows = new();
    private readonly Stack<Image> _tempImagePool = new();
    private readonly Stack<Image> _fxImagePool = new();
    private readonly Stack<GoalRowView> _animalGoalRowPool = new();
    private readonly Stack<GoalRowView> _primaryGoalRowPool = new();
    private readonly Dictionary<GoalRowView, bool> _isPrimaryGoalRow = new();

    public Func<Vector2Int, bool> CanStartSwap;

    //public void ShowMoves(bool show) => _movesCountText.gameObject.SetActive(show);

    public void Build(int width, int height)
    {
        _width = width;
        _height = height;

        // Change the grid layout according to the board config
        if(gridLayout == null || _boardParent == null || _cell == null || _defaultSprite == null)
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

                // Subscribe to raw input events
                cellView.PointerDown += OnCellPointerDown;
                cellView.Drag += OnCellDrag;
                cellView.PointerUp += OnCellPointerUp;

                _cells.Add(coord, cellView);
            }
        }

    }

    // Assign the sprites to the animals on the board
    public void AssignSprites(Board board)
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
            }
        }
    }
    public void SetLevelNumber(int level)
    {
        _levelNumberText.text = level.ToString();
    }
    public void SetBackground(Sprite backgroundSprite)
    {
        if (_backgroundImage == null)
        {
            Debug.LogWarning("BoardView: background image is missing.");
            return;
        }

        _backgroundImage.sprite = backgroundSprite;
    }
    public void SwapCellVisuals(Vector2Int a, Vector2Int b)
    {
        if (!_cells.ContainsKey(a) || !_cells.ContainsKey(b))
        {
            Debug.LogWarning($"SwapCellVisuals failed: {a} or {b} not found");
            return;
        }

        var aView = _cells[a];
        var bView = _cells[b];

        var aSprite = aView.CurrentSprite;
        var aColor = aView.CurrentColor;

        aView.SetSprite(bView.CurrentSprite, bView.CurrentColor);
        bView.SetSprite(aSprite, aColor);
    }
    public void ShowGoal(bool show)
    {
        if (_goalRowsParent != null)
            _goalRowsParent.gameObject.SetActive(show);
    }
    public void SetScore(int points, int totalPoints)
    {
        ApplyGoalRows(new List<GoalRowSpec>
    {
        new GoalRowSpec
        {
            IsPrimary = true,
            Icon = null,
            Text = $"Points: {points}/{totalPoints}",
            Color = Color.white,
            IsComplete = points >= totalPoints
        }
    });
    }
    public void SetMatchedAnimals(int animals, int goal)
    {
        int remaining = Mathf.Max(0, goal - animals);

        ApplyGoalRows(new List<GoalRowSpec>
    {
        new GoalRowSpec
        {
            IsPrimary = true,
            Icon = null,
            Text = $"Matches: {remaining}",
            Color = Color.white,
            IsComplete = animals >= goal
        }
    });
    }
    public void SetCollectGoals(List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        var specs = new List<GoalRowSpec>();

        foreach (var g in goals)
        {
            if (g.animal == null)
                continue;

            collected.TryGetValue(g.animal._id, out int have);
            int remaining = Mathf.Max(0, g.amount - have);
            bool isComplete = have >= g.amount;

            specs.Add(new GoalRowSpec
            {
                IsPrimary = false,
                Icon = g.animal._sprite,
                Text = remaining.ToString(),
                Color = g.animal.color,
                IsComplete = isComplete
            });
        }

        ApplyGoalRows(specs);
    }

    private void ClearGoalRows()
    {
        for (int i = 0; i < _rows.Count; i++)
            ReleaseGoalRow(_rows[i]);

        _rows.Clear();
    }
    private void OnCellPointerDown(Vector2Int coord, Vector2 screenPos)
    {
        if (CanStartSwap != null && !CanStartSwap(coord))
        {
            _gestureActive = false;
            _swipeCommitted = false;

            _startCell = coord;
            _startScreenPos = screenPos;

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(4);

            _ = AnimateBlockedTap(coord);
            return;
        }

        _gestureActive = true;
        _swipeCommitted = false;
        _startCell = coord;
        _startScreenPos = screenPos;

        SetHighlightedCell(coord);
    }

    private void OnCellDrag(Vector2Int coord, Vector2 screenPos)
    {
        TryCommitSwipe(screenPos);
    }

    private void OnCellPointerUp(Vector2Int coord, Vector2 screenPos)
    {
        TryCommitSwipe(screenPos);

        if (!_swipeCommitted)
            CellTapped?.Invoke(coord);

        _gestureActive = false;
        ClearHighlightedCell();
    }
    private void TryCommitSwipe(Vector2 currentScreenPos)
    {
        if (!_gestureActive || _swipeCommitted || !SwapsEnabled) return;

        Vector2 delta = currentScreenPos - _startScreenPos;

        if (delta.magnitude < SwipeThresholdPixels)
            return;

        Vector2Int dir;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            dir = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            dir = delta.y > 0 ? Vector2Int.down : Vector2Int.up;

        var to = _startCell + dir;

        _swipeCommitted = true;
        ClearHighlightedCell();

        if (!IsInBounds(to))
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(4); // can't swap sound

            _ = AnimateInvalidSwap(_startCell, null, dir);
            return;
        }

        SwapRequested?.Invoke(_startCell, to);
    }

    private bool IsInBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < _width && cell.y >= 0 && cell.y < _height;
    }
    
    public void SetMovesText(int movesLeft)
    {
        _movesCountText.text = movesLeft.ToString();
    }

    public void SetTimerVisible(bool visible)
    {
        _timerPowerUp.gameObject.SetActive(visible);
        _timerBackground.gameObject.SetActive(visible);
    }

    public void SetTimerSeconds(int seconds)
    {
        _timerPowerUp.text = $"Match Time!\n{seconds}";
    }

    // Dotween animation
    public Task AnimateSwap(Vector2Int a, Vector2Int b, float duration = 0.18f)
    {
        if (!_cells.ContainsKey(a) || !_cells.ContainsKey(b))
            return Task.CompletedTask;

        var aView = _cells[a];
        var bView = _cells[b];

        // Create 2 temporary images that can move freely
        Image tempA = CreateTempImage(aView);
        Image tempB = CreateTempImage(bView);

        // Hide the real images during animation
        aView.SetImageEnabled(false);
        bView.SetImageEnabled(false);

        var tcs = new TaskCompletionSource<bool>(); // Create a future task that I’ll mark as finished later.

        Sequence seq = DOTween.Sequence(); // Create a sequence of animations
        seq.Join(tempA.rectTransform.DOMove(bView.ImageRect.position, duration).SetEase(Ease.InOutQuad));
        seq.Join(tempB.rectTransform.DOMove(aView.ImageRect.position, duration).SetEase(Ease.InOutQuad));

        seq.OnComplete(() =>
        {
            // Swap the real sprites at the end
            var aSprite = aView.CurrentSprite;
            var aColor = aView.CurrentColor;

            aView.SetSprite(bView.CurrentSprite, bView.CurrentColor);
            bView.SetSprite(aSprite, aColor);

            aView.SetImageEnabled(true);
            bView.SetImageEnabled(true);

            ReleaseTempImage(tempA);
            ReleaseTempImage(tempB);

            tcs.SetResult(true);
        });

        return tcs.Task;
    }

    private Image CreateTempImage(CellView source)
    {
        var img = GetPooledImage(_tempImagePool, "SwapTemp");
        var rt = img.rectTransform;

        img.sprite = source.CurrentSprite;
        img.color = source.CurrentColor;

        // Match screen position + size
        rt.position = source.ImageRect.position;
        rt.rotation = source.ImageRect.rotation;
        rt.sizeDelta = source.ImageRect.rect.size;
        rt.localScale = Vector3.one;

        return img;
    }
    private Image GetPooledImage(Stack<Image> pool, string objectName)
    {
        Image img;

        if (pool.Count > 0)
        {
            img = pool.Pop();
        }
        else
        {
            var go = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(_swapOverlay, worldPositionStays: false);
            img = go.GetComponent<Image>();
            img.raycastTarget = false;
        }

        img.gameObject.name = objectName;
        img.transform.SetParent(_swapOverlay, worldPositionStays: false);
        img.gameObject.SetActive(true);

        img.DOKill();
        img.rectTransform.DOKill();

        var rt = img.rectTransform;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition3D = Vector3.zero;

        img.sprite = null;
        img.color = Color.white;
        img.raycastTarget = false;

        return img;
    }
    private void ReleaseTempImage(Image img)
    {
        ReleasePooledImage(img, _tempImagePool);
    }

    private void ReleaseFxImage(Image img)
    {
        ReleasePooledImage(img, _fxImagePool);
    }
    private void ReleasePooledImage(Image img, Stack<Image> pool)
    {
        if (img == null)
            return;

        img.DOKill();
        img.rectTransform.DOKill();

        img.sprite = null;
        img.color = Color.white;

        var rt = img.rectTransform;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
        rt.anchoredPosition3D = Vector3.zero;

        img.gameObject.SetActive(false);
        pool.Push(img);
    }
    private GoalRowView GetGoalRow(GoalRowView prefab, bool isPrimary)
    {
        var pool = isPrimary ? _primaryGoalRowPool : _animalGoalRowPool;
        GoalRowView row;

        if (pool.Count > 0)
        {
            row = pool.Pop();
        }
        else
        {
            row = Instantiate(prefab, _goalRowsParent);
        }

        row.transform.SetParent(_goalRowsParent, false);
        row.gameObject.SetActive(true);
        _isPrimaryGoalRow[row] = isPrimary;
        return row;
    }

    private void ReleaseGoalRow(GoalRowView row)
    {
        if (row == null)
            return;

        row.gameObject.SetActive(false);

        bool isPrimary = _isPrimaryGoalRow.TryGetValue(row, out bool cachedIsPrimary) && cachedIsPrimary;
        if (isPrimary)
            _primaryGoalRowPool.Push(row);
        else
            _animalGoalRowPool.Push(row);
    }
    // For level 10
    public void SetPointsAndCollectGoals(int points, int pointsGoal, List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        var specs = new List<GoalRowSpec>
    {
        new GoalRowSpec
        {
            IsPrimary = true,
            Icon = null,
            Text = $"Points: {points}/{pointsGoal}",
            Color = Color.white,
            IsComplete = points >= pointsGoal
        }
    };

        foreach (var g in goals)
        {
            if (g.animal == null)
                continue;

            collected.TryGetValue(g.animal._id, out int have);
            int remaining = Mathf.Max(0, g.amount - have);
            bool isComplete = have >= g.amount;

            specs.Add(new GoalRowSpec
            {
                IsPrimary = false,
                Icon = g.animal._sprite,
                Text = remaining.ToString(),
                Color = g.animal.color,
                IsComplete = isComplete
            });
        }

        ApplyGoalRows(specs);
    }

    public void SetMatchesAndCollectGoals(int matched, int matchGoal, List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        int remainingMatches = Mathf.Max(0, matchGoal - matched);

        var specs = new List<GoalRowSpec>
    {
        new GoalRowSpec
        {
            IsPrimary = true,
            Icon = null,
            Text = remainingMatches.ToString(),
            Color = Color.white,
            IsComplete = matched >= matchGoal
        }
    };

        foreach (var g in goals)
        {
            if (g.animal == null)
                continue;

            collected.TryGetValue(g.animal._id, out int have);
            int remaining = Mathf.Max(0, g.amount - have);
            bool isComplete = have >= g.amount;

            specs.Add(new GoalRowSpec
            {
                IsPrimary = false,
                Icon = g.animal._sprite,
                Text = remaining.ToString(),
                Color = g.animal.color,
                IsComplete = isComplete
            });
        }

        ApplyGoalRows(specs);
    }

    // Animate the gravity 
    public Task AnimateGravity(List<Board.FallMove> moves, List<Board.SpawnInfo> spawns, Board board, float duration = 0.20f)
    {
        bool hasMoves = moves != null && moves.Count > 0;
        bool hasSpawns = spawns != null && spawns.Count > 0;

        if (!hasMoves && !hasSpawns)
        {
            AssignSprites(board);
            return Task.CompletedTask;
        }

        // Hide all involved cells so we only see the moving temp images
        var involved = new HashSet<Vector2Int>();
        if (hasMoves)
        {
            foreach (var m in moves) { involved.Add(m.from); involved.Add(m.to); }
        }
        if (hasSpawns)
        {
            foreach (var s in spawns) involved.Add(s.cell);
        }

        foreach (var c in involved)
            if (_cells.TryGetValue(c, out var cv))
                cv.SetImageEnabled(false);

        var temps = new List<Image>();
        var tcs = new TaskCompletionSource<bool>();

        Sequence seq = DOTween.Sequence();

        // Falling moves
        if (hasMoves)
        {
            foreach (var m in moves)
            {
                if (!_cells.ContainsKey(m.from) || !_cells.ContainsKey(m.to)) continue;

                var fromView = _cells[m.from];
                var toView = _cells[m.to];

                var temp = CreateTempImage(fromView);
                temps.Add(temp);

                seq.Join(temp.rectTransform
                    .DOMove(toView.ImageRect.position, duration)
                    .SetEase(Ease.InQuad));
            }
        }

        // Spawns
        if (hasSpawns)
        {
            foreach (var s in spawns)
            {
                if (!_cells.ContainsKey(s.cell) || s.animal == null) continue;

                var targetView = _cells[s.cell];
                var targetPos = targetView.ImageRect.position;

                int entryY = (s.spawnFromY >= 0) ? (s.spawnFromY + 1) : 0;

                // Create temp image
                var temp = CreateTempImageFromSprite(s.animal._sprite, s.animal.color, targetView);
                temps.Add(temp);

                float cellH = targetView.ImageRect.rect.height;
                float cellW = targetView.ImageRect.rect.width;
                float upOffset = cellH * 1.2f;

                if (s.spawnFromY < 0)
                {
                    // Normal: spawn from above board in same column
                    var startView = _cells[new Vector2Int(s.cell.x, 0)];
                    temp.rectTransform.position = startView.ImageRect.position + Vector3.up * upOffset;

                    seq.Join(temp.rectTransform
                        .DOMove(targetPos, duration)
                        .SetEase(Ease.InQuad));
                }
                else
                {
                    // Better gravity feel for pieces coming from under a blocker:
                    // start slightly ABOVE the entry cell and let them accelerate down.
                    // No fade-in, because fade looks like "appearing", not falling.

                    var entryCell = new Vector2Int(s.cell.x, entryY);
                    var entryView = _cells[entryCell];

                    float startAbove = cellH * 0.42f;

                    temp.rectTransform.position = entryView.ImageRect.position + Vector3.up * startAbove;
                    temp.rectTransform.localScale = new Vector3(0.94f, 1.08f, 1f);
                    temp.color = Color.white;

                    Sequence sseq = DOTween.Sequence();

                    sseq.Append(
                        temp.rectTransform
                            .DOMove(targetPos, duration)
                            .SetEase(Ease.InQuad)
                    );

                    sseq.Join(
                        temp.rectTransform
                            .DOScale(Vector3.one, duration * 0.35f)
                            .SetEase(Ease.OutQuad)
                    );

                    seq.Join(sseq);
                }
            }
        }

        seq.OnComplete(() =>
        {
            // Redraw final state
            AssignSprites(board);

            // Re-enable real cell images
            foreach (var c in involved)
                if (_cells.TryGetValue(c, out var cv))
                    cv.SetImageEnabled(true);

            // Cleanup temps
            for (int i = 0; i < temps.Count; i++)
                ReleaseTempImage(temps[i]);

            tcs.SetResult(true);
        });

        return tcs.Task;
    }
    private Tween BuildMatchLikePop(RectTransform pieceRt, float duration = 0.12f)
    {
        pieceRt.DOKill();
        pieceRt.localScale = Vector3.one;

        Sequence pop = DOTween.Sequence();
        pop.Append(pieceRt.DOScale(1.12f, duration * 0.28f).SetEase(Ease.OutQuad));
        pop.Append(pieceRt.DOScale(0.82f, duration * 0.42f).SetEase(Ease.InBack));

        return pop;
    }
    private Image CreateTempImageFromSprite(Sprite sprite, Color color, CellView sizeReference)
    {
        var img = GetPooledImage(_tempImagePool, "FallTemp");
        var rt = img.rectTransform;

        img.sprite = sprite;
        img.color = color;

        rt.sizeDelta = sizeReference.ImageRect.rect.size;
        rt.localScale = Vector3.one;

        return img;
    }
    public Task AnimateHint(Vector2Int a, Vector2Int b, float duration = 0.2f)
    {
        if (!_cells.ContainsKey(a) || !_cells.ContainsKey(b))
            return Task.CompletedTask;

        var aRt = _cells[a].ImageRect;
        var bRt = _cells[b].ImageRect;

        aRt.DOKill();
        bRt.DOKill();

        aRt.localScale = Vector3.one;
        bRt.localScale = Vector3.one;

        var tcs = new TaskCompletionSource<bool>();

        Sequence seq = DOTween.Sequence();
        seq.Join(aRt.DOScale(1.15f, duration).SetLoops(4, LoopType.Yoyo));
        seq.Join(bRt.DOScale(1.15f, duration).SetLoops(4, LoopType.Yoyo));

        seq.OnComplete(() =>
        {
            aRt.localScale = Vector3.one;
            bRt.localScale = Vector3.one;
            tcs.TrySetResult(true);
        });

        seq.OnKill(() =>
        {
            aRt.localScale = Vector3.one;
            bRt.localScale = Vector3.one;
            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateMatchPopFx(List<Vector2Int> matches, float duration = 0.12f)
    {
        if (matches == null || matches.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        var spawnedFx = new List<Image>();
        var touchedCells = new List<RectTransform>();

        Sequence master = DOTween.Sequence();

        foreach (var cell in matches)
        {
            if (!_cells.TryGetValue(cell, out var cv))
                continue;

            var pieceRt = cv.ImageRect;
            touchedCells.Add(pieceRt);

            pieceRt.DOKill();
            pieceRt.localScale = Vector3.one;

            // Main piece pop
            master.Join(BuildMatchLikePop(pieceRt, duration));

            Vector3 center = pieceRt.position;
            Vector2 size = pieceRt.rect.size;

            // White ring
            if (_matchRingSprite != null)
            {
                Image ring = CreateFxImage(_matchRingSprite, _matchFxColor, center, size * 0.95f);
                spawnedFx.Add(ring);

                var ringRt = ring.rectTransform;
                ringRt.localScale = Vector3.one * 0.55f;

                master.Join(ringRt.DOScale(1.45f, duration).SetEase(Ease.OutQuad));
                master.Join(ring.DOFade(0f, duration).SetEase(Ease.OutQuad));
            }

            // Sparkles
            if (_sparkleSprite != null)
            {
                float baseDist = Mathf.Min(size.x, size.y) * 0.28f;

                for (int i = 0; i < _sparklesPerMatch; i++)
                {
                    Vector2 dir = Random.insideUnitCircle.normalized;
                    if (dir.sqrMagnitude < 0.01f)
                        dir = Vector2.up;

                    float dist = baseDist * Random.Range(0.8f, 1.15f);
                    float sparkDur = duration * Random.Range(0.75f, 1.0f);

                    Image spark = CreateFxImage(_sparkleSprite, _matchFxColor, center, size * 0.18f);
                    spawnedFx.Add(spark);

                    var sparkRt = spark.rectTransform;
                    sparkRt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                    master.Join(
                        sparkRt.DOMove(center + (Vector3)(dir * dist), sparkDur)
                               .SetEase(Ease.OutQuad)
                    );

                    master.Join(
                        sparkRt.DOScale(Random.Range(0.35f, 0.7f), sparkDur)
                               .SetEase(Ease.InQuad)
                    );

                    master.Join(
                        spark.DOFade(0f, sparkDur)
                             .SetEase(Ease.OutQuad)
                    );
                }
            }
        }

        master.OnComplete(() =>
        {
            foreach (var rt in touchedCells)
                if (rt != null)
                    rt.localScale = Vector3.one;

            foreach (var fx in spawnedFx)
                ReleaseFxImage(fx);

            tcs.TrySetResult(true);
        });

        master.OnKill(() =>
        {
            foreach (var rt in touchedCells)
                if (rt != null)
                    rt.localScale = Vector3.one;

            foreach (var fx in spawnedFx)
                ReleaseFxImage(fx);

            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task ShowShuffleMessage(string text = "No more moves!", float hold = 0.9f)
    {
        if (_shuffleMessage == null)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        _shuffleMessage.DOKill();
        _shuffleMessage.text = text;
        _shufflePopUp.SetActive(true);

        var rt = _shuffleMessage.rectTransform;
        var cg = _shuffleMessage.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = _shuffleMessage.gameObject.AddComponent<CanvasGroup>();

        rt.localScale = Vector3.one * 0.75f;
        cg.alpha = 0f;

        DOTween.Sequence()
            .Append(rt.DOScale(1f, 0.18f).SetEase(Ease.OutBack))
            .Join(cg.DOFade(1f, 0.12f))
            .AppendInterval(hold)
            .Append(rt.DOScale(0.9f, 0.16f).SetEase(Ease.InBack))
            .Join(cg.DOFade(0f, 0.16f))
            .OnComplete(() =>
            {
                _shufflePopUp.gameObject.SetActive(false);
                tcs.TrySetResult(true);
            });

        return tcs.Task;
    }

    public Task AnimateShuffle(Board board, float outDuration = 0.15f, float inDuration = 0.2f, float stagger = 0.002f)
    {
        if (board == null || _cells.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        var orderedCells = new List<CellView>();

        foreach (var kvp in _cells)
            orderedCells.Add(kvp.Value);

        // Randomize order so the board doesn't disappear row-by-row every time
        for (int i = orderedCells.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (orderedCells[i], orderedCells[j]) = (orderedCells[j], orderedCells[i]);
        }

        Sequence seq = DOTween.Sequence();

        // OUT: shrink old animals away one by one
        for (int i = 0; i < orderedCells.Count; i++)
        {
            var rt = orderedCells[i].ImageRect;
            rt.DOKill();
            rt.localScale = Vector3.one;

            seq.Join(
                rt.DOScale(0f, outDuration)
                  .SetDelay(i * stagger)
                  .SetEase(Ease.InBack)
            );
        }

        // Swap sprites only after old board is fully hidden
        seq.AppendCallback(() =>
        {
            AssignSprites(board);

            for (int i = 0; i < orderedCells.Count; i++)
            {
                var rt = orderedCells[i].ImageRect;
                rt.localScale = Vector3.zero;
            }
        });

        // Pop new shuffled board back in one by one
        for (int i = 0; i < orderedCells.Count; i++)
        {
            var rt = orderedCells[i].ImageRect;

            seq.Join(
                rt.DOScale(1f, inDuration)
                  .SetDelay(i * stagger)
                  .SetEase(Ease.OutBack)
            );
        }

        seq.OnComplete(() =>
        {
            for (int i = 0; i < orderedCells.Count; i++)
                orderedCells[i].ImageRect.localScale = Vector3.one;

            tcs.TrySetResult(true);
        });

        seq.OnKill(() =>
        {
            for (int i = 0; i < orderedCells.Count; i++)
                orderedCells[i].ImageRect.localScale = Vector3.one;

            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateBombImpact(List<Vector2Int> affected, float duration = 0.28f)
    {
        if (affected == null || affected.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        var spawnedFx = new List<Image>();

        Sprite ringSprite = _bombRingSprite != null ? _bombRingSprite : _matchRingSprite;
        if (ringSprite == null)
            return Task.CompletedTask;

        Sequence master = DOTween.Sequence();

        foreach (var coord in affected)
        {
            if (!_cells.TryGetValue(coord, out var cv))
                continue;

            RectTransform pieceRt = cv.ImageRect;
            pieceRt.DOKill();
            pieceRt.localScale = Vector3.one;

            Vector3 center = pieceRt.position;
            Vector2 size = pieceRt.rect.size;

            Image ring = CreateFxImage(ringSprite, _bombRingColor, center, size * 1.08f);
            spawnedFx.Add(ring);

            var ringRt = ring.rectTransform;
            ringRt.localScale = Vector3.one * 0.72f;

            Color startColor = _bombRingColor;
            startColor.a = 0f;
            ring.color = startColor;

            Sequence one = DOTween.Sequence();

            // Ring appears
            one.Append(ring.DOFade(_bombRingColor.a, duration * 0.22f).SetEase(Ease.OutQuad));
            one.Join(ringRt.DOScale(1.08f, duration * 0.22f).SetEase(Ease.OutQuad));

            // Short hold so the player reads the impacted cells
            one.AppendInterval(duration * 0.18f);

            // Ring fade + exact match-like pop at the END
            one.Append(ring.DOFade(0f, duration * 0.30f).SetEase(Ease.InQuad));
            one.Join(ringRt.DOScale(1.22f, duration * 0.30f).SetEase(Ease.OutQuad));

            // Same feel as match pop, but ends at zero so it doesn't stick half-shrunk
            one.Join(pieceRt.DOScale(1.12f, 0.12f * 0.28f).SetEase(Ease.OutQuad));
            one.Append(pieceRt.DOScale(0f, 0.12f * 0.42f).SetEase(Ease.InBack));

            master.Join(one);
        }

        master.OnComplete(() =>
        {
            foreach (var fx in spawnedFx)
                ReleaseFxImage(fx);

            tcs.TrySetResult(true);
        });

        master.OnKill(() =>
        {
            foreach (var kvp in _cells)
            {
                if (kvp.Value != null && kvp.Value.ImageRect != null)
                    kvp.Value.ImageRect.localScale = Vector3.one;
            }

            foreach (var fx in spawnedFx)
                ReleaseFxImage(fx);

            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }

    public Task AnimateBombWarning(Vector2Int coord, float totalDuration = 1.5f)
    {
        if (!_cells.TryGetValue(coord, out var cv))
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        RectTransform rt = cv.ImageRect;
        Image img = cv.CellImage;

        Color originalColor = img.color;
        Color warmColor = new Color(1f, 0.82f, 0.35f, 1f);
        Color dangerColor = new Color(1f, 0.35f, 0.35f, 1f);

        rt.DOKill();
        img.DOKill();

        rt.localScale = Vector3.one;
        img.color = originalColor;

        int pulses = 6;
        float halfStep = totalDuration / (pulses * 2f);

        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < pulses; i++)
        {
            float t = (i + 1f) / pulses;
            float scale = Mathf.Lerp(1.03f, 1.16f, t);
            Color flashColor = Color.Lerp(warmColor, dangerColor, t);

            seq.Append(rt.DOScale(scale, halfStep).SetEase(Ease.OutQuad));
            seq.Join(img.DOColor(flashColor, halfStep));

            seq.Append(rt.DOScale(1f, halfStep).SetEase(Ease.InQuad));
            seq.Join(img.DOColor(originalColor, halfStep));
        }

        seq.OnComplete(() =>
        {
            rt.localScale = Vector3.one;
            img.color = originalColor;
            tcs.TrySetResult(true);
        });

        seq.OnKill(() =>
        {
            rt.localScale = Vector3.one;
            img.color = originalColor;
            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }



    private Image CreateFxImage(Sprite sprite, Color color, Vector3 position, Vector2 size)
    {
        var img = GetPooledImage(_fxImagePool, "MatchFX");
        var rt = img.rectTransform;

        img.sprite = sprite;
        img.color = color;

        rt.position = position;
        rt.sizeDelta = size;
        rt.localScale = Vector3.one;

        return img;
    }

    public Vector3 GetCellWorldPosition(Vector2Int coord)
    {
        if (_cells.TryGetValue(coord, out var cell))
            return cell.ImageRect.position;

        return Vector3.zero;
    }

    public Vector3 GetCellScenePosition(Vector2Int coord, Camera worldCamera, float worldZ = 0f)
    {
        if (!_cells.TryGetValue(coord, out var cell) || worldCamera == null)
            return Vector3.zero;

        Vector3 screenPoint = RectTransformUtility.WorldToScreenPoint(null, cell.ImageRect.position);

        float camDistance = Mathf.Abs(worldCamera.transform.position.z - worldZ);
        Vector3 worldPoint = worldCamera.ScreenToWorldPoint(
            new Vector3(screenPoint.x, screenPoint.y, camDistance)
        );

        worldPoint.z = worldZ;
        return worldPoint;
    }

    public Transform GetFxParent()
    {
        return _swapOverlay != null ? _swapOverlay : transform;
    }

    public Task AnimateInvalidSwap(Vector2Int a, Vector2Int? b = null, Vector2Int? dir = null, float duration = 0.20f)
    {
        var targets = new List<(RectTransform rt, bool isPrimary)>();

        if (_cells.TryGetValue(a, out var aView))
            targets.Add((aView.ImageRect, true));

        if (b.HasValue && _cells.TryGetValue(b.Value, out var bView))
            targets.Add((bView.ImageRect, false));

        if (targets.Count == 0)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        int completed = 0;

        // Board direction -> UI direction
        // x stays the same
        // y is inverted because your board coordinates go downward
        Vector2 primaryOffset = dir.HasValue
            ? new Vector2(dir.Value.x, -dir.Value.y) * 16f
            : new Vector2(18f, 0f); // fallback if no direction known

        Vector2 secondaryOffset = -primaryOffset * 0.65f;

        foreach (var target in targets)
        {
            var rt = target.rt;
            rt.DOKill();

            Vector2 startPos = rt.anchoredPosition;
            Vector3 startScale = rt.localScale;

            Vector2 moveOffset = target.isPrimary ? primaryOffset : secondaryOffset;

            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOAnchorPos(startPos + moveOffset, duration * 0.28f).SetEase(Ease.OutQuad));
            seq.Join(rt.DOScale(startScale * 1.04f, duration * 0.20f).SetEase(Ease.OutQuad));

            seq.Append(rt.DOAnchorPos(startPos, duration * 0.72f).SetEase(Ease.OutBack));
            seq.Join(rt.DOScale(startScale, duration * 0.60f).SetEase(Ease.OutQuad));

            seq.OnComplete(() =>
            {
                rt.anchoredPosition = startPos;
                rt.localScale = startScale;

                completed++;
                if (completed >= targets.Count)
                    tcs.TrySetResult(true);
            });

            seq.OnKill(() =>
            {
                rt.anchoredPosition = startPos;
                rt.localScale = startScale;

                completed++;
                if (completed >= targets.Count)
                    tcs.TrySetResult(true);
            });
        }

        return tcs.Task;
    }

    public Task AnimateBlockedTap(Vector2Int cell, float duration = 0.12f)
    {
        if (!_cells.TryGetValue(cell, out var cv))
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();

        RectTransform rt = cv.ImageRect;
        Image img = cv.CellImage;

        rt.DOKill();
        img.DOKill();

        Vector3 startScale = rt.localScale;
        Color startColor = img.color;
        Color flashColor = Color.Lerp(startColor, new Color(1f, 0.8f, 0.8f, 1f), 0.35f);

        Sequence seq = DOTween.Sequence();
        seq.Append(rt.DOScale(startScale * 0.94f, duration * 0.35f).SetEase(Ease.OutQuad));
        seq.Join(img.DOColor(flashColor, duration * 0.35f));
        seq.Append(rt.DOScale(startScale, duration * 0.65f).SetEase(Ease.OutBack));
        seq.Join(img.DOColor(startColor, duration * 0.65f));

        seq.OnComplete(() =>
        {
            rt.localScale = startScale;
            img.color = startColor;
            tcs.TrySetResult(true);
        });

        seq.OnKill(() =>
        {
            rt.localScale = startScale;
            img.color = startColor;
            tcs.TrySetResult(true);
        });

        return tcs.Task;
    }
    private void SetHighlightedCell(Vector2Int? coord)
    {
        if (_highlightedCell.HasValue &&
            _cells.TryGetValue(_highlightedCell.Value, out var oldCell))
        {
            oldCell.SetHighlighted(false);
        }

        _highlightedCell = coord;

        if (_highlightedCell.HasValue &&
            _cells.TryGetValue(_highlightedCell.Value, out var newCell))
        {
            newCell.SetHighlighted(true);
        }
    }

    private void ClearHighlightedCell()
    {
        SetHighlightedCell(null);
    }
    private void AddGoalRow(GoalRowView prefab, Sprite icon, string text, Color color, bool isComplete = false)
    {
        if (prefab == null || _goalRowsParent == null)
            return;

        bool isPrimary = prefab == _primaryGoalRowPrefab;
        var row = GetGoalRow(prefab, isPrimary);
        row.Set(icon, text, color, isComplete);
        _rows.Add(row);
    }

    private void AddPrimaryGoalRow(string text, bool isComplete = false)
    {
        AddGoalRow(_primaryGoalRowPrefab, null, text, Color.white, isComplete);
    }

    private void AddAnimalGoalRow(Sprite icon, string text, Color color, bool isComplete = false)
    {
        AddGoalRow(_animalGoalRowPrefab, icon, text, color, isComplete);
    }

    private void ApplyGoalRows(List<GoalRowSpec> specs)
    {
        if (!GoalLayoutMatches(specs))
            RebuildGoalRows(specs);

        for (int i = 0; i < specs.Count; i++)
        {
            var spec = specs[i];
            var row = _rows[i];
            row.transform.SetSiblingIndex(i);
            row.Set(spec.Icon, spec.Text, spec.Color, spec.IsComplete);
        }
    }

    private bool GoalLayoutMatches(List<GoalRowSpec> specs)
    {
        if (_rows.Count != specs.Count)
            return false;

        for (int i = 0; i < specs.Count; i++)
        {
            if (_rows[i] == null)
                return false;

            bool rowIsPrimary = _isPrimaryGoalRow.TryGetValue(_rows[i], out bool cachedIsPrimary) && cachedIsPrimary;
            if (rowIsPrimary != specs[i].IsPrimary)
                return false;
        }

        return true;
    }

    private void RebuildGoalRows(List<GoalRowSpec> specs)
    {
        ClearGoalRows();

        for (int i = 0; i < specs.Count; i++)
        {
            var spec = specs[i];
            var prefab = spec.IsPrimary ? _primaryGoalRowPrefab : _animalGoalRowPrefab;
            var row = GetGoalRow(prefab, spec.IsPrimary);
            row.transform.SetSiblingIndex(i);
            _rows.Add(row);
        }
    }
}
