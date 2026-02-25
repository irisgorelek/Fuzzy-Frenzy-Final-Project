using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Runtime.CompilerServices;
using NUnit.Framework;
using UnityEditor;
using DG.Tweening;
using System.Threading.Tasks;

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

    [Header("Sprite")]
    [SerializeField] private Sprite _defaultSprite; // For null animals

    [Header("On Screen Texts")]
    [SerializeField] private TextMeshProUGUI _goal;
    [SerializeField] private TextMeshProUGUI _movesCountText;
    [SerializeField] private TextMeshProUGUI _timerPowerUp;
    [SerializeField] private Transform _goalRowsParent;
    [SerializeField] private GoalRowView _goalRowPrefab;


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
    private const float SwipeThresholdPixels = 45f;

    private readonly List<GoalRowView> _rows = new();


    public void ShowGoal(bool show) => _goal.gameObject.SetActive(show);

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

                if(animal == null)
                {
                    _cells[coord].SetSprite(_defaultSprite, Color.red);
                    continue;
                }

                _cells[coord].SetSprite(animal._sprite, animal.color);
            }
        }
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

        Debug.Log($"Swapping visuals {a} <-> {b}");
        Debug.Log($"Before: A={aView.CurrentSprite?.name}, B={bView.CurrentSprite?.name}");

        var aSprite = aView.CurrentSprite;
        var aColor = aView.CurrentColor;

        aView.SetSprite(bView.CurrentSprite, bView.CurrentColor);
        bView.SetSprite(aSprite, aColor);
        Debug.Log($"After:  A={aView.CurrentSprite?.name}, B={bView.CurrentSprite?.name}");
    }
    public void SetScore(int points, int totalPoints)
    {
        ClearGoalRows();
        Debug.LogWarning($"Set Score: {points} / {totalPoints}");
        _goal.text = $"Points: {points} / {totalPoints}";
    }
    public void SetMatchedAnimals(int animals, int goal)
    {
        ClearGoalRows();
        Debug.LogWarning($"Set Score: {animals} / {goal}");
        _goal.text = $"Matched: {animals} / {goal}";
    }
    public void SetCollectGoals(List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        ClearGoalRows();
        //_goal.text = "Collect:"; // header

        foreach (var g in goals)
        {
            if (g.animal == null) continue;

            collected.TryGetValue(g.animal._id, out int have);

            var row = Instantiate(_goalRowPrefab, _goalRowsParent);
            row.Set(g.animal._sprite, $"{have}/{g.amount}".Trim() , g.animal.color);
            _rows.Add(row);
        }
    }
    private void ClearGoalRows()
    {
        for (int i = 0; i < _rows.Count; i++)
            Destroy(_rows[i].gameObject);
        _rows.Clear();
    }
    //public void SetPointsRow(int points, int goal, Sprite pointsIcon = null)
    //{
    //    ClearGoalRows();
    //    _goal.text = "Goal:";

    //    var row = Instantiate(_goalRowPrefab, _goalRowsParent);
    //    row.Set(pointsIcon, $"Points: {points}/{goal}");
    //    _rows.Add(row);
    //}
    private void OnCellPointerDown(Vector2Int coord, Vector2 screenPos)
    {
        Debug.Log($"PointerDown on {coord}");
        _gestureActive = true;
        _swipeCommitted = false;
        _startCell = coord;
        _startScreenPos = screenPos;
    }

    private void OnCellDrag(Vector2Int coord, Vector2 screenPos)
    {
        TryCommitSwipe(screenPos);
    }

    private void OnCellPointerUp(Vector2Int coord, Vector2 screenPos)
    {
        TryCommitSwipe(screenPos);

        // If no swipe happened, treat it as a tap
        if (!_swipeCommitted)
            CellTapped?.Invoke(coord);

        _gestureActive = false;
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

        _swipeCommitted = true; // Commit once per gesture

        if (!IsInBounds(to))
            return;

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
    }

    public void SetTimerSeconds(int seconds)
    {
        _timerPowerUp.text = $"Timer: {seconds}";
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

            Destroy(tempA.gameObject);
            Destroy(tempB.gameObject);

            tcs.SetResult(true);
        });

        return tcs.Task;
    }

    private Image CreateTempImage(CellView source)
    {
        var go = new GameObject("SwapTemp", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(_swapOverlay, worldPositionStays: false);

        var img = go.GetComponent<Image>();
        img.sprite = source.CurrentSprite;
        img.color = source.CurrentColor;
        img.raycastTarget = false;

        var rt = (RectTransform)go.transform;

        // Match screen position + size
        rt.position = source.ImageRect.position;
        rt.rotation = source.ImageRect.rotation;
        rt.sizeDelta = source.ImageRect.rect.size;
        rt.localScale = Vector3.one;

        return img;
    }

    // For level 10
    public void SetPointsAndCollectGoals(int points, int pointsGoal, List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        ClearGoalRows();
        _goal.text = "Goals:";

        // Points row (no icon)
        var pointsRow = Instantiate(_goalRowPrefab, _goalRowsParent);
        pointsRow.Set(null, $"Points: {points}/{pointsGoal}", Color.white);
        _rows.Add(pointsRow);

        // Collect rows
        foreach (var g in goals)
        {
            if (g.animal == null) continue;

            collected.TryGetValue(g.animal._id, out int have);

            var row = Instantiate(_goalRowPrefab, _goalRowsParent);
            row.Set(g.animal._sprite, $"{g.animal._id}: {have}/{g.amount}", g.animal.color);
            _rows.Add(row);
        }
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

        var temps = new List<GameObject>();
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
                temps.Add(temp.gameObject);

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
                temps.Add(temp.gameObject);

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
                    // Under a blocker: spawn from side and slide under the bone row
                    int leftX = s.cell.x - 1;
                    int rightX = s.cell.x + 1;

                    // pick a side (simple + safe)
                    int sideX =
                        (leftX >= 0 && rightX < _width)
                            ? (UnityEngine.Random.value < 0.5f ? leftX : rightX)
                            : (leftX >= 0 ? leftX : rightX);

                    // reference cell on that side, at the entry row
                    var sideCell = new Vector2Int(sideX, entryY);
                    var sideView = _cells[sideCell];

                    // start slightly outside the grid on that side, and slightly above
                    Vector3 sideDir = (sideX < s.cell.x) ? Vector3.left : Vector3.right;
                    temp.rectTransform.position =
                        sideView.ImageRect.position + Vector3.up * upOffset + sideDir * (cellW * 0.8f);

                    // “slide under” pivot: same y as entry row, x of target
                    Vector3 pivot = new Vector3(targetPos.x, sideView.ImageRect.position.y, targetPos.z);

                    Sequence sseq = DOTween.Sequence();
                    sseq.Append(temp.rectTransform.DOMove(pivot, duration * 0.35f).SetEase(Ease.OutQuad));
                    sseq.Append(temp.rectTransform.DOMove(targetPos, duration * 0.65f).SetEase(Ease.InQuad));

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
                Destroy(temps[i]);

            tcs.SetResult(true);
        });

        return tcs.Task;
    }

    private Image CreateTempImageFromSprite(Sprite sprite, Color color, CellView sizeReference)
    {
        var go = new GameObject("FallTemp", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(_swapOverlay, worldPositionStays: false);

        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;

        var rt = (RectTransform)go.transform;
        rt.sizeDelta = sizeReference.ImageRect.rect.size;
        rt.localScale = Vector3.one;

        return img;
    }
}
