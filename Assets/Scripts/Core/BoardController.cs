using UnityEngine;
using System.Threading.Tasks;
using NUnit.Framework;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using TMPro;

public class BoardController : MonoBehaviour
{
    [Header("Board Parameters")] 
    [SerializeField] private BoardConfig _cfg;
    [SerializeField] private BoardView _view;
    [SerializeField] private MoveCounter _moveCounter;

    [Header("On Screen Pop Ups")]
    [SerializeField] private GameObject _levelClearedPopup;
    [SerializeField] private GameObject _levelLostPopup;

    [SerializeField] private LevelCompletedEventChannelSO _levelCompletedChannelSO;
    [SerializeField] private AnimalsDestroyedEventChannelSO _animalsDestroyedChannelSO;
    [SerializeField] private ScoreEventChannelSO _scoreEventChannelSO;

    private Board _board;
    private bool _isBusy; // If an animation is going, or in the middle of a swap/cascade 
    private bool _isLevelOver = false;

    // Timer bomb parameters
    public bool IsTimerBombActive { get; private set; }
    private float _timerBombEndTime;
    private bool _timerBombResolving;

    public int GetWidth() => _cfg.weidth;
    public int GetHeight() => _cfg.height;

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

    private void Update()
    {
        if (!IsTimerBombActive || _timerBombResolving)
        {
            return;
        }

        UpdateTimerUI();

        if (Time.time >= _timerBombEndTime)
        {
            EndTimerBomb();
        }
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

        _view.SetTimerVisible(false);
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

        _board.OnAnimalsDestroyed = (animalId, amount) =>
        {
            _animalsDestroyedChannelSO.RaiseEvent(animalId, amount);
        };

        _board.OnScoreAdded = amount => _scoreEventChannelSO.RaiseEvent(amount);

        _moveCounter.InitializeMoves(_cfg.maxMoves);
        _board.Initialize();
        
        // Visual
        _view.Build(_cfg.weidth, _cfg.height);
        _view.AssignSprites(_board);
    }

    public async void OnSwapRequested(Vector2Int from, Vector2Int to)
    {
        Debug.Log($"SwapRequested: {from} -> {to}");
        if (_isBusy || _isLevelOver) return;
        if (!IsTimerBombActive && _moveCounter.MovesLeft <= 0) return;

        // Do if activated timer power up
        if (IsTimerBombActive)
        {
            bool swapped = _board.SwapCellsRaw(from, to);
            if (!swapped) return;

            _view.SwapCellVisuals(from, to);
            return;
        }


        // Do if normal gameplay
        _isBusy = true;

        // Show the swap immediately even if invalid
        _view.SwapCellVisuals(from, to);

        // Let the swap show in unity
        await WaitFrames(10);

        bool swappedNormal = _board.SwapCellsRaw(from, to);

        if (!swappedNormal)
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

    public async void TryRemoveCellsFromGrid(List<Vector2Int> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            TryRemoveCell(cells[i]);
        }

        _board.ApplyGravity();
        _board.Refill();

        _view.AssignSprites(_board);

        await ResolveCascadesAsync(25);
    }

    public void StartTimerBomb(float durationSeconds)
    {
        if (_isLevelOver) return;

        IsTimerBombActive = true;
        _timerBombResolving = false;
        _timerBombEndTime = Time.time + durationSeconds;

        _view.SetTimerVisible(true);
        UpdateTimerUI();

        // allow swiping during the timer
        _view.SwapsEnabled = true;
    }

    private async void EndTimerBomb()
    {
        _timerBombResolving = true;
        IsTimerBombActive = false;

        _view.SetTimerVisible(false);

        // freeze input while resolving
        _view.SwapsEnabled = false;
        _isBusy = true;

        await ResolveCascadesAsync(25);

        _isBusy = false;
        _view.SwapsEnabled = true;
        _timerBombResolving = false;
    }

    private void TryRemoveCell(Vector2Int cell)
    {
        if (!_board.IsCellInBounds(cell))
        {
            Debug.Log($"The cell [x:{cell.x}, y: {cell.y}] is not in bounds, couldn't remove");
            return;
        }

        _board.ClearGridCell(cell);
        UpdateGoalUI();
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
                _levelCompletedChannelSO.RaiseEvent(_cfg.level);
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
    private void UpdateTimerUI()
    {
        float remaining = _timerBombEndTime - Time.time;
        remaining = Mathf.Max(0f, remaining);

        int seconds = Mathf.CeilToInt(remaining);
        _view.SetTimerSeconds(seconds);
    }
}
