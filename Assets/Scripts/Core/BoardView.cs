using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardView : MonoBehaviour
{
    [SerializeField] private RectTransform _boardParent;
    [SerializeField] private GameObject _cell;
    [SerializeField] Color _selectedColor, _normalColor; // For highlighting 
    [SerializeField] private Sprite _defaultSprite; // For null animals
    [SerializeField] private GridLayoutGroup gridLayout;

    private Dictionary<Vector2Int, CellView> _cells = new();
    int _width, _height;


    public event Action<Vector2Int, Vector2Int> SwapRequested;

    // Swipe state
    private bool _gestureActive;
    private bool _swipeCommitted;
    private Vector2Int _startCell;
    private Vector2 _startScreenPos;
    private const float SwipeThresholdPixels = 45f;

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

    public void ApplyHighlight(Vector2Int? coord)
    {

    }
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

        _gestureActive = false;
        //_swipeCommitted = false;
    }
    private void TryCommitSwipe(Vector2 currentScreenPos)
    {
        if (!_gestureActive || _swipeCommitted) return;

        Vector2 delta = currentScreenPos - _startScreenPos;

        if (delta.magnitude < SwipeThresholdPixels)
            return;

        Vector2Int dir;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            dir = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            dir = delta.y > 0 ? Vector2Int.down : Vector2Int.up;

        var to = _startCell + dir;

        _swipeCommitted = true; // IMPORTANT: commit once per gesture

        if (!IsInBounds(to))
            return;

        SwapRequested?.Invoke(_startCell, to);
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
    private bool IsInBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < _width && cell.y >= 0 && cell.y < _height;
    }
}
