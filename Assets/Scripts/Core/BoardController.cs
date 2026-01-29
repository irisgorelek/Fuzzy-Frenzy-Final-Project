using UnityEngine;
using System.Threading.Tasks;

public class BoardController : MonoBehaviour
{
    [SerializeField] private BoardConfig _cfg;
    [SerializeField] private BoardView _view;
    [SerializeField] private GameObject _levelClearedPopup;
    [SerializeField] private GameObject _levelLostPopup;
    [SerializeField] private MoveCounter _moveCounter;

    private Board _board;
    private bool _isBusy; // If an animation is going, or in the middle of a swap/cascade 
    private bool _isLevelOver = false;

    private void Awake()
    {
        _levelClearedPopup.gameObject.SetActive(false);
        _levelLostPopup.gameObject.SetActive(false);
    }

    public void Start()
    {
        if (_cfg == null || _view == null)
            Debug.LogError("Error: Either cfg or view weren't inserted in the board controller");

        InitializeGame();

        // Show initial goal/progress
        UpdateGoalUI();
    }

    private void OnEnable()
    {
        if (_view != null)
            _view.SwapRequested += OnSwapRequested;
        if (_moveCounter != null && _view != null)
        {
            _moveCounter.OnMovesChanged += _view.SetMovesText;
            _view.SetMovesText(_moveCounter.MovesLeft);
        }
    }

    private void OnDisable()
    {
        if (_view != null)
            _view.SwapRequested -= OnSwapRequested;

        if (_moveCounter != null && _view != null)
            _moveCounter.OnMovesChanged -= _view.SetMovesText;
    }

    public void InitializeGame()
    {
        // Technical
        _board = new Board(_cfg);
        _moveCounter.InitializeMoves(_cfg.maxMoves);
        _board.Initialize();
        
        // Visual
        _view.Build(_cfg.weidth, _cfg.height);
        _view.AssignSprites(_board);
    }

    public async void OnSwapRequested(Vector2Int from, Vector2Int to)
    {
        Debug.Log($"SwapRequested: {from} -> {to}");
        if (_isBusy || _moveCounter.MovesLeft <= 0 || _isLevelOver) return;

        _isBusy = true;

        // Show the swap immediately even if invalid
        _view.SwapCellVisuals(from, to);

        // Let the swap show in unity
        await WaitFrames(10);

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

        _moveCounter.UseMove();

        // The swap was valid. Resolve cascades with pacing
        _view.AssignSprites(_board); // Sync view to model after swap
        
        await ResolveCascadesAsync(25);

        if (_isLevelOver)
        {
            _isBusy = false;
            return;
        }

        if (_moveCounter.MovesLeft <= 0)
        {
            _isLevelOver = true;
            if (_levelLostPopup != null) _levelLostPopup.SetActive(true);
            _isBusy = false;
            return;
        }

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

            // Points + Amount of animals matched
            UpdateGoalUI();

            // Redraw
            _view.AssignSprites(_board);

            if (_board.IsGoalReached)
            {
                _levelClearedPopup.gameObject.SetActive(true);
                _isLevelOver = true;
                return;
            }

            // Check again
            matches = _board.FindMatches();
        }
    }

    private void UpdateGoalUI()
    {
        if (_board.GoalType == PointsOrMatches.points)
        {
            _view.ShowPoints(true); // Show the points text
            _view.ShowAnimals(false); // Don't show the animals text
            _view.SetScore(_board.CurrentPoints, _board.GoalAmount);
        }
        else
        {
            _view.ShowPoints(false); // Don't show the points text
            _view.ShowAnimals(true); // Show the animals text
            _view.SetMatchedAnimals(_board.MatchedAnimals, _board.GoalAmount);
        }
    }

    private async Task WaitFrames(int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
            await Task.Yield();
    }
}
