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

    //private int cellSelectionCounter = 0;
    //private Vector2Int firstCell;

    public void Start()
    {
        if(_cfg == null || _view == null)
        {
            Debug.LogError("Error: Either cfg or view weren't inserted");
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

        // Ask the model if it was valid(model will revert internally if invalid)
        bool valid = _board.TrySwapCells(from, to);

        if (valid)
        {
            // TODO: Trigger animations

            // redraw whole board from model (temporary, until animations)
            _view.AssignSprites(_board);

        }
        else
        {
            // revert visuals (swap back)
            _view.SwapCellVisuals(from, to);
        }

        //_view.AssignSprites(_board); // TODO: Probably should make it await for animation

        _isBusy = false;
    }

    private async Task WaitFrames(int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
            await Task.Yield();
    }
}
