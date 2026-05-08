using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class BoardViewGrid
{
    private readonly Dictionary<Vector2Int, CellView> _cells;
    private readonly Sprite _defaultSprite;
    private Vector2Int? _highlightedCell;
    private int _width;
    private int _height;

    public IReadOnlyDictionary<Vector2Int, CellView> Cells => _cells;
    public int Width => _width;
    public int Height => _height;

    public BoardViewGrid(Dictionary<Vector2Int, CellView> cells, Sprite defaultSprite)
    {
        _cells = cells;
        _defaultSprite = defaultSprite;
    }

    public void Build(
        int width,
        int height,
        RectTransform boardParent,
        GridLayoutGroup gridLayout,
        GameObject cellPrefab,
        Color selectedColor,
        Color normalColor,
        Color tutorialLockedColor,
        Action<Vector2Int, Vector2> onPointerDown,
        Action<Vector2Int, Vector2> onDrag,
        Action<Vector2Int, Vector2> onPointerUp)
    {
        _width = width;
        _height = height;

        // Change the grid layout according to the board config
        if (gridLayout == null || boardParent == null || cellPrefab == null || _defaultSprite == null)
        {
            Debug.LogError("Something in BoardView is null");
            return;
        }

        gridLayout.constraintCount = _width;

        // Clear old
        foreach (Transform child in boardParent)
            UnityEngine.Object.Destroy(child.gameObject);

        _cells.Clear();

        for (int y = 0; y < _height; y++)
        {
            for (int x = 0; x < _width; x++)
            {
                var coord = new Vector2Int(x, y);

                var go = UnityEngine.Object.Instantiate(cellPrefab, boardParent);
                var cellView = go.GetComponent<CellView>();
                cellView.Init(coord);
                cellView.ConfigureHighlight(selectedColor, normalColor);
                cellView.ConfigureTutorialLock(tutorialLockedColor);

                // Subscribe to raw input events
                cellView.PointerDown += onPointerDown;
                cellView.Drag += onDrag;
                cellView.PointerUp += onPointerUp;

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

    public void RefreshCellSprite(Vector2Int coord, Board board)
    {
        if (!_cells.TryGetValue(coord, out var cellView))
            return;

        var animal = board.GetAnimalFromCell(coord);

        if (animal == null)
        {
            cellView.SetSprite(_defaultSprite, Color.red);
            return;
        }

        cellView.SetSprite(animal._sprite, animal.color);
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

    public bool IsInBounds(Vector2Int cell)
    {
        return cell.x >= 0 && cell.x < _width && cell.y >= 0 && cell.y < _height;
    }

    public bool TryGetCell(Vector2Int coord, out CellView cellView)
    {
        return _cells.TryGetValue(coord, out cellView);
    }

    public List<CellView> GetOrderedCells()
    {
        var orderedCells = new List<CellView>();

        foreach (var kvp in _cells)
            orderedCells.Add(kvp.Value);

        return orderedCells;
    }

    public Vector3 GetCellWorldPosition(Vector2Int coord)
    {
        if (_cells.TryGetValue(coord, out var cell))
            return cell.ImageRect.position;

        return Vector3.zero;
    }

    public void SetTutorialLockedCell(Vector2Int? coord)
    {
        foreach (var kvp in _cells)
            kvp.Value.SetTutorialLocked(false);

        if (coord.HasValue && _cells.TryGetValue(coord.Value, out var cell))
            cell.SetTutorialLocked(true);
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

    public void SetHighlightedCell(Vector2Int? coord)
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

    public void ClearHighlightedCell()
    {
        SetHighlightedCell(null);
    }
}
