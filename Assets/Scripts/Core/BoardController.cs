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
    [SerializeField] private LevelClearedPopupUI _levelClearedPopupUI;
    [SerializeField] private GameObject _levelLostPopup;
    [SerializeField] private int framesBetweenSteps = 5;

    [Header("Rewards Configs")]
    [SerializeField] private RewardsConfig _rewards;
    [SerializeField] private BootstrapperLocator _locator; // to add coins

    [SerializeField] private LevelCompletedEventChannelSO _levelCompletedChannelSO;
    [SerializeField] private AnimalsDestroyedEventChannelSO _animalsDestroyedChannelSO;
    [SerializeField] private ScoreEventChannelSO _scoreEventChannelSO;

    [SerializeField] private LevelScoreEventChannelSO _levelScoreEventChannelSO;

    public BoardConfig Config => _cfg;

    private Board _board;
    private bool _isBusy; // If an animation is going, or in the middle of a swap/cascade 
    private bool _isLevelOver = false;

    private Dictionary<string, int> _collected = new Dictionary<string, int>(); // Track collected animals

    // Timer bomb parameters
    public bool IsTimerBombActive { get; private set; }
    private float _timerBombEndTime;
    private bool _timerBombResolving;

    public int GetWidth() => _cfg.weidth;
    public int GetHeight() => _cfg.height;
    private bool HasCollectGoals => _cfg.collectGoals != null && _cfg.collectGoals.Count > 0;

    private void Awake()
    {
        //_levelClearedPopup.gameObject.SetActive(false);
        //_levelLostPopup.gameObject.SetActive(false);

        var bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        if (bootstrapper != null && bootstrapper.SelectedLevel != null)
            _cfg = bootstrapper.SelectedLevel;
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

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayBG((int)_cfg.songNumber);
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

        _collected.Clear();
        _board.OnAnimalsDestroyed = HandleAnimalsDestroyed;

        _board.OnScoreAdded = amount => _scoreEventChannelSO.RaiseEvent(amount);
        _board.OnScoreAdded = amount => _levelScoreEventChannelSO.RaiseEvent(amount); // In-Level

        _moveCounter.InitializeMoves(_cfg.maxMoves);
        _board.Initialize();

        //_blackSheepTriggered = false;

        // Visual
        _view.Build(_cfg.weidth, _cfg.height);
        _view.AssignSprites(_board);
    }

    public async void OnSwapRequested(Vector2Int from, Vector2Int to)
    {
        Debug.Log($"SwapRequested: {from} -> {to}");
        if (_isBusy || _isLevelOver) return;
        if (!IsTimerBombActive && _moveCounter.MovesLeft <= 0) return;

        var a = _board.GetAnimalFromCell(from);
        var b = _board.GetAnimalFromCell(to);

        if (a == null || b == null) 
            return;

        if (!a._canSwap || !b._canSwap)
            return;

        // Do if activated timer power up
        if (IsTimerBombActive)
        {
            bool swapped = _board.SwapCellsRaw(from, to);
            if (!swapped) return;

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFXPitchAdjusted(12, 0.2f); // Play swap sound.
            }
            await _view.AnimateSwap(from, to, 0.18f);
            return;
        }

        // Do if normal gameplay
        _isBusy = true;

        bool sheepSwiped = IsAnySheep(a); // started swipe on a sheep
        bool otherIsSheep = IsAnySheep(b); // or you swiped into a sheep
        bool sheepInvolved = sheepSwiped || otherIsSheep;

        bool swipeVertical = (from.x == to.x);

        // where the sheep ends up after the swap
        Vector2Int sheepPosAfterSwap = sheepSwiped ? to : otherIsSheep ? from : from;

        if (!_board.SwapCellsRaw(from, to))
        {
            // Out of bounds / not neighbors
            _isBusy = false;

            if (AudioManager.instance != null)
            {
                AudioManager.instance.PlaySFX(4); // Play can't swap sound.
            }

            return;
        }

        // Show the swap immediately even if invalid
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXPitchAdjusted(12, 0.2f); // Play swap sound.
        }
        await _view.AnimateSwap(from, to, 0.18f);

        // Let the swap show in unity
        // await WaitFrames(framesBetweenSteps);

        if (sheepInvolved) // Black sheep
        {
            _moveCounter.UseMove();

            TryRollBlackSheep();

            _board.TriggerSheepSwipeBlast(sheepPosAfterSwap, swipeVertical);

            UpdateGoalUI();
            _view.AssignSprites(_board);

            await ResolveCascadesAsync(framesBetweenSteps);

            _isBusy = false;
            Debug.Log($"a={a?._id}, b={b?._id}, sheepInvolved={sheepInvolved}");
            return;
        }

        // Check if the swap didn't produce any match
        if (!_board.HasAnyMatch())
        {
            // The swap was invalid. Swap back in model and animate back
            _board.SwapCellsRaw(from, to);
            await _view.AnimateSwap(from, to, 0.18f); // animate back

            _isBusy = false;
            return;
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXPitchAdjusted(8, 0.2f); // Play pop sound.
        }
        _moveCounter.UseMove();
        TryRollBlackSheep(); // Roll the black sheep spawn

        // The swap was valid. Resolve cascades with pacing
        _view.AssignSprites(_board); // Sync view to model after swap
        
        await ResolveCascadesAsync(framesBetweenSteps);

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

    private void TryRollBlackSheep()
    {
        if (_cfg.blackSheep == null) return; // not this level

        int movesMade = _cfg.maxMoves - _moveCounter.MovesLeft;

        if (_cfg.blackSheepUnlockAfterMoves <= 0) return;

        if (movesMade % _cfg.blackSheepUnlockAfterMoves == 0)
        {
            _board.RollForBlackSheep(_cfg.blackSheepRollChance);
            Debug.Log($"Rolled for black sheep at move {movesMade}");
        }
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

        await ResolveCascadesAsync(10);
    }

    public void StartTimerBomb(float durationSeconds)
    {
        if (_isLevelOver) return;

        IsTimerBombActive = true;
        _timerBombResolving = false;
        _timerBombEndTime = Time.time + durationSeconds;

        _view.SetTimerVisible(true);
        UpdateTimerUI();

        if (AudioManager.instance != null)
            AudioManager.instance.PlayTimerMusic();

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

        if (AudioManager.instance != null)
            AudioManager.instance.PlayBG((int)_cfg.songNumber);

        await ResolveCascadesAsync(10);

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

            var fallMoves = new List<Board.FallMove>();
            var spawns = new List<Board.SpawnInfo>();

            _board.ResolveMatches(matches, fallMoves, spawns);

            UpdateGoalUI();

            // animate gravity and refill
            await _view.AnimateGravity(fallMoves, spawns, _board, 0.20f);

            // Points + Amount of animals matched
            UpdateGoalUI();

            // Redraw
            //_view.AssignSprites(_board);

            if (AreAllGoalsComplete())
            {
                _levelCompletedChannelSO.RaiseEvent(_cfg.levelIndex);
                int movesUsed = _cfg.maxMoves - _moveCounter.MovesLeft;
                int stars = _rewards.GetStars(_cfg.maxMoves, movesUsed);
                int coins = _rewards.GetCoins(stars, _cfg.levelIndex);
                int finalScore = _board.CurrentPoints;
                int level = _cfg.levelIndex;

                _locator.Bootstrapper.Economy.AddCoins(coins);

                //_levelClearedPopup.gameObject.SetActive(true);
                _levelClearedPopupUI.Show(level, finalScore, coins, stars);    // show with script to integrate text and animations
                Debug.Log($"LevelClearedPopupUI.Show called: score={finalScore}, coins={coins}, stars={stars}, MovesUsed={movesUsed}");

                if (AudioManager.instance != null)
                {
                    AudioManager.instance.ChangeMusicVolume(0.4f);
                    AudioManager.instance.PlaySFX(16);
                    await WaitFrames(25); // Wait until sound is over
                    AudioManager.instance.ChangeMusicVolume(1f);
                }

                _isLevelOver = true;
                return;
            }

            // Check again
            matches = _board.FindMatches();
        }
    }

    private void UpdateGoalUI()
    {
        _view.ShowGoal(true); // Show the goal text

        // last-level style - points and collectGoals
        if (_cfg.goalType == PointsOrMatches.points && HasCollectGoals)
        {
            _view.SetPointsAndCollectGoals(_board.CurrentPoints, _cfg.goal, _cfg.collectGoals, _collected);
            return;
        }

        if (_board.GoalType == PointsOrMatches.points)
        {
            _view.SetScore(_board.CurrentPoints, _board.GoalAmount);
        }
        else if (_board.GoalType == PointsOrMatches.matches)
        {
            _view.SetMatchedAnimals(_board.MatchedAnimals, _board.GoalAmount);
        }
        else if (_cfg.goalType == PointsOrMatches.collectAnimals)
        {
            _view.SetCollectGoals(_cfg.collectGoals, _collected);
        }
    }

    // For the animal collection goal
    private void HandleAnimalsDestroyed(string animalId, int count)
    {
        _animalsDestroyedChannelSO.RaiseEvent(animalId, count);

        // Only track collection if this level is a collect level
        if (!HasCollectGoals) // && _cfg.goalType != PointsOrMatches.collectAnimals)
            return;

        if (!_collected.TryGetValue(animalId, out int have))
            have = 0;

        _collected[animalId] = have + count;

        // Update goal UI
        UpdateGoalUI();
    }

    private bool IsCollectGoalComplete()
    {
        foreach (var g in _cfg.collectGoals)
        {
            if (g.animal == null) continue;

            _collected.TryGetValue(g.animal._id, out int have);
            if (have < g.amount) return false;
        }
        return true;
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
    private bool AreAllGoalsComplete()
    {
        // points or total matches
        bool primaryComplete = _cfg.goalType == PointsOrMatches.collectAnimals ? IsCollectGoalComplete() : _board.IsGoalReached;

        bool collectComplete = HasCollectGoals ? IsCollectGoalComplete() : true;

        return primaryComplete && collectComplete;
    }

    private bool IsAnimal(Animal piece, Animal target)
    {
        return piece != null && target != null && piece._id == target._id;
    }
    private bool IsAnySheep(Animal piece)
    {
        return IsAnimal(piece, _cfg.blackSheep);
    }
}
