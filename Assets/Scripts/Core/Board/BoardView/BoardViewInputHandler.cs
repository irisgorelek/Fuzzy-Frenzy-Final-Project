using System;
using UnityEngine;

public sealed class BoardViewInputHandler
{
    private const float SwipeThresholdPixels = 45f;

    private readonly BoardViewGrid _grid;
    private readonly Func<bool> _swapsEnabled;
    private readonly Func<Vector2Int, Vector2Int?, Vector2Int?, float, System.Threading.Tasks.Task> _animateInvalidSwap;
    private readonly Func<Vector2Int, float, System.Threading.Tasks.Task> _animateBlockedTap;

    private bool _gestureActive;
    private bool _swipeCommitted;
    private bool _suppressNextCellTap;
    private Vector2Int _startCell;
    private Vector2 _startScreenPos;

    public Func<Vector2Int, bool> CanStartSwap;
    public event Action<Vector2Int, Vector2Int> SwapRequested;
    public event Action<Vector2Int> CellTapped;

    public BoardViewInputHandler(
        BoardViewGrid grid,
        Func<bool> swapsEnabled,
        Func<Vector2Int, Vector2Int?, Vector2Int?, float, System.Threading.Tasks.Task> animateInvalidSwap,
        Func<Vector2Int, float, System.Threading.Tasks.Task> animateBlockedTap)
    {
        _grid = grid;
        _swapsEnabled = swapsEnabled;
        _animateInvalidSwap = animateInvalidSwap;
        _animateBlockedTap = animateBlockedTap;
    }

    public void OnCellPointerDown(Vector2Int coord, Vector2 screenPos)
    {
        if (CanStartSwap != null && !CanStartSwap(coord))
        {
            _gestureActive = false;
            _swipeCommitted = false;
            _suppressNextCellTap = true;

            _startCell = coord;
            _startScreenPos = screenPos;

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(4);

            _ = _animateBlockedTap(coord, 0.12f);
            return;
        }

        _suppressNextCellTap = false;
        _gestureActive = true;
        _swipeCommitted = false;
        _startCell = coord;
        _startScreenPos = screenPos;

        _grid.SetHighlightedCell(coord);
    }

    public void OnCellDrag(Vector2Int coord, Vector2 screenPos)
    {
        TryCommitSwipe(screenPos);
    }

    public void OnCellPointerUp(Vector2Int coord, Vector2 screenPos)
    {
        TryCommitSwipe(screenPos);

        if (_suppressNextCellTap)
        {
            _suppressNextCellTap = false;
            _gestureActive = false;
            _grid.ClearHighlightedCell();
            return;
        }

        if (!_swipeCommitted)
            CellTapped?.Invoke(coord);

        _gestureActive = false;
        _grid.ClearHighlightedCell();
    }

    private void TryCommitSwipe(Vector2 currentScreenPos)
    {
        if (!_gestureActive || _swipeCommitted || !_swapsEnabled()) return;

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
        _grid.ClearHighlightedCell();

        if (!_grid.IsInBounds(to))
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(4); // can't swap sound

            _ = _animateInvalidSwap(_startCell, null, dir, 0.20f);
            return;
        }

        SwapRequested?.Invoke(_startCell, to);
    }
}
