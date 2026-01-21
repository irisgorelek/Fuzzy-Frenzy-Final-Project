using System;
using UnityEngine;
using System.Threading.Tasks;

public class BoardController : MonoBehaviour
{
    [SerializeField] private BoardConfig _cfg;
    [SerializeField] private BoardView _view;

    private Board _board;
    private Vector2Int? _selectedCell;
    private bool _isBusy; // If an animation is going, or in the middle of a swap/cascade 

    public void Start()
    {
        if(_cfg == null || _view == null)
        {
            Debug.LogError("Error: Either cfg or view weren't inserted in the board controller");
        }

        InitializeGame();
    }

    private void OnEnable()
    {
        if (_view != null)
            _view.SwapRequested += OnSwapRequested;
    }

    private void OnDisable()
    {
        if (_view != null)
            _view.SwapRequested -= OnSwapRequested;
    }

    public void InitializeGame()
    {
        // Technical
        _board = new Board(_cfg);
        _board.Initialize();

        // Visual
        _view.Build(_cfg.weidth, _cfg.height);
        _view.AssignSprites(_board);
    }

    public async void OnSwapRequested(Vector2Int from, Vector2Int to)
    {
        Debug.Log($"SwapRequested: {from} -> {to}");
        if (_isBusy) return;

        _isBusy = true;

        // Show the swap immediately even if invalid
        _view.SwapCellVisuals(from, to);

        // Let the swap show in unity
        await WaitFrames(30);

        bool swapped = _board.SwapCellsRaw(from, to);

        if (!swapped)
        {
            // Out of bounds / not neighbors
            _view.SwapCellVisuals(from, to); // swap back
            _isBusy = false;
            return;
        }

        // Check if the swap didn't produce any match
        if (!_board.HasAnyMatch())
        {
            // The swap was invalid. Swap back in model and view
            _board.SwapCellsRaw(from, to);
            _view.SwapCellVisuals(from, to);
            _isBusy = false;
            return;
        }

        // The swap was valid. Resolve cascades with pacing
        _view.AssignSprites(_board); // sync view to model after swap
        await ResolveCascadesAsync(25);

        _isBusy = false;
    }

    private async Task ResolveCascadesAsync(int framesBetweenSteps)
    {
        var matches = _board.FindMatches();
        while (matches.Count > 0)
        {
            // Let the player see the current state before clearing
            await WaitFrames(framesBetweenSteps);

            // Resolve clear + gravity + refill
            _board.ResolveMatches(matches);

            // Redraw
            _view.AssignSprites(_board);

            // Check again
            matches = _board.FindMatches();
        }
    }

    private async Task WaitFrames(int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
            await Task.Yield();
    }
}
