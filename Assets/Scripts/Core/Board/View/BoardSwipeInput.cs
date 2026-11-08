using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Turns raw pointer events from the cells into swipes and taps. A swipe past the threshold commits a swap
/// request in its main direction; a press on a tile that can't move gets blocked-tap feedback instead.
/// </summary>
public sealed class BoardSwipeInput
{
    private const float SwipeThresholdPixels = 45f;

    private readonly BoardView _view;
    private readonly Dictionary<Vector2Int, CellView> _cells;
    private readonly IGameAudio _audio;

    private bool _gestureActive;
    private bool _swipeCommitted;
    private bool _suppressNextCellTap;
    private Vector2Int _startCell;
    private Vector2 _startScreenPos;
    private Vector2Int? _highlightedCell;

    public BoardSwipeInput(BoardView view, Dictionary<Vector2Int, CellView> cells, IGameAudio audio)
    {
        _view = view;
        _cells = cells;
        _audio = audio;
    }

    public void Attach(CellView cell)
    {
        cell.PointerDown += OnPointerDown;
        cell.Drag += OnDrag;
        cell.PointerUp += OnPointerUp;
    }

    private void OnPointerDown(Vector2Int coord, Vector2 screenPos)
    {
        _startCell = coord;
        _startScreenPos = screenPos;
        _swipeCommitted = false;

        if (!_view.CanStartSwapAt(coord))
        {
            _gestureActive = false;
            _suppressNextCellTap = true;

            PlayCannotSwapSfx();
            _ = _view.AnimateBlockedTap(coord);
            return;
        }

        _suppressNextCellTap = false;
        _gestureActive = true;
        SetHighlightedCell(coord);
    }

    private void OnDrag(Vector2Int coord, Vector2 screenPos) => TryCommitSwipe(screenPos);

    private void OnPointerUp(Vector2Int coord, Vector2 screenPos)
    {
        TryCommitSwipe(screenPos);

        if (_suppressNextCellTap)
        {
            _suppressNextCellTap = false;
            _gestureActive = false;
            SetHighlightedCell(null);
            return;
        }

        if (!_swipeCommitted)
            _view.RaiseCellTapped(coord);

        _gestureActive = false;
        SetHighlightedCell(null);
    }

    private void TryCommitSwipe(Vector2 currentScreenPos)
    {
        if (!_gestureActive || _swipeCommitted || !_view.SwapsEnabled) return;

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
        SetHighlightedCell(null);

        if (!_cells.ContainsKey(to))
        {
            PlayCannotSwapSfx();
            _ = _view.AnimateInvalidSwap(_startCell, null, dir);
            return;
        }

        _view.RaiseSwapRequested(_startCell, to);
    }

    private void SetHighlightedCell(Vector2Int? coord)
    {
        if (_highlightedCell.HasValue && _cells.TryGetValue(_highlightedCell.Value, out var oldCell))
            oldCell.SetHighlighted(false);

        _highlightedCell = coord;

        if (_highlightedCell.HasValue && _cells.TryGetValue(_highlightedCell.Value, out var newCell))
            newCell.SetHighlighted(true);
    }

    private void PlayCannotSwapSfx() => _audio.PlaySfx(SfxId.CannotSwap);
}
