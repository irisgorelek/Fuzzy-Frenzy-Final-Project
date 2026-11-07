using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;
using UnityEngine;

/// <summary>
/// Unity entry point for a level. Turns input into calls on <see cref="Match3Game"/>, replays each
/// <see cref="TurnResult"/> through <see cref="TurnPresenter"/>, and handles what happens around a turn:
/// moves, goals UI, win/lose, the timer booster and dialogue. All input goes through one <see cref="InputGate"/>.
/// </summary>
public class BoardController : MonoBehaviour, ITurnPresentationListener
{
    [Header("Board Parameters")]
    [SerializeField] private BoardConfig _cfg;
    [SerializeField] private BoardView _view;
    [SerializeField] private MoveCounter _moveCounter;

    [Header("On Screen Pop Ups")]
    [SerializeField] private LevelClearedPopupUI _levelClearedPopupUI;
    [SerializeField] private GameObject _levelClearedDimmer;
    [SerializeField] private LevelLostPopupUI _levelLostPopupUI;
    [SerializeField] private LevelVFXToggle _levelVFXToggle;

    [Header("Rewards Configs")]
    [SerializeField] private RewardsConfig _rewards;
    [SerializeField] private BootstrapperLocator _locator;  // Services communication

    [SerializeField] private LevelCompletedEventChannelSO _levelCompletedChannelSO;
    [SerializeField] private AnimalsDestroyedEventChannelSO _animalsDestroyedChannelSO;
    [SerializeField] private LevelScoreEventChannelSO _levelScoreEventChannelSO;

    [Header("Dialogue")]
    [SerializeField] private AnimalDialogueController _animalDialogueController;

    public float _speakSfxDuration = 1f;

    // AudioManager SFX ids
    private const int SfxCannotSwap = 4;
    private const int SfxLevelCleared = 16;
    private const int SfxTimerStart = 13;

    private readonly InputGate _input = new InputGate();
    private readonly BoardHintFinder _hintFinder = new BoardHintFinder();

    private Board _board;
    private Match3Game _game;
    private TurnPresenter _presenter;

    private bool _isLevelOver;
    private IDisposable _levelOverLock;
    private bool _swapsSuppressed;

    private Animal _debugBlackSheep;
    private Animal _debugBoneBlock;

    // Goal progress as the player has seen it so far. The core is ahead of this while a turn replays.
    private int _shownPoints;
    private int _shownMatched;
    private readonly Dictionary<string, int> _collected = new Dictionary<string, int>();

    // Timer booster
    private float _timerBombEndTime;
    private bool _timerBombResolving;
    private int _lastShownTimerSecond = -1;
    private Task _timerSwapTask = Task.CompletedTask;

    public Action<bool> OnTimerBombStateChanged;

    public BoardConfig Config => _cfg;
    public Board CurrentBoard => _board;
    public bool IsTimerBombActive { get; private set; }

    /// <summary>True while a tutorial or a triggered bubble is showing.</summary>
    public bool IsSpeechBubbleInputBlocked => _animalDialogueController != null && _animalDialogueController.IsBlockingInput;

    /// <summary>True while a turn, booster or shuffle is playing out.</summary>
    public bool IsBusy => !_input.IsOpen;

    /// <summary>Whether a swap, booster or hint may start right now.</summary>
    public bool CanAcceptInput => _board != null && _input.IsOpen && !_isLevelOver && !IsSpeechBubbleInputBlocked;

    public int GetWidth() => _cfg.weidth;
    public int GetHeight() => _cfg.height;

    private Animal BlackSheep => _cfg.blackSheep != null ? _cfg.blackSheep : _debugBlackSheep;
    private Animal BoneBlock => _cfg.boneBlock != null ? _cfg.boneBlock : _debugBoneBlock;
    private bool HasCollectGoals => _cfg.collectGoals != null && _cfg.collectGoals.Count > 0;

    // ---------------- Lifecycle ----------------

    private void Awake()
    {
        _input.Changed += _ => ApplySwapsEnabled();

        var bootstrapper = GameBootstrapper.Instance;
        if (bootstrapper != null && bootstrapper.SelectedLevel != null)
            _cfg = bootstrapper.SelectedLevel;
    }

    public async void Start()
    {
        if (_cfg == null || _view == null)
            Debug.LogError("Error: Either cfg or view weren't inserted in the board controller");

        InitializeGame();
        UpdateGoalUI();

        await EnsurePlayableBoardAsync();
        await TryStartLevelDialogueAsync();
    }

    private void Update()
    {
        if (!IsTimerBombActive || _timerBombResolving)
            return;

        UpdateTimerUI();

        if (Time.time >= _timerBombEndTime || _board.Session.AreAllGoalsComplete)
            EndTimerBomb();
    }

    private void OnEnable()
    {
        if (_view != null)
        {
            _view.SwapRequested += OnSwapRequested;
            _view.CanStartSwap = CanStartSwapAt;
        }

        if (_moveCounter != null && _view != null)
        {
            _moveCounter.OnMovesChanged += OnMovesChangedForView;
            OnMovesChangedForView(_moveCounter.MovesLeft);
        }

        _lastShownTimerSecond = -1;
        if (_view != null)
        {
            _view.SetTimerVisible(IsTimerBombActive);
            if (IsTimerBombActive)
                UpdateTimerUI();
        }

        if (AudioManager.instance != null && _cfg != null)
            AudioManager.instance.PlayBG((int)_cfg.songNumber);
    }

    private void OnDisable()
    {
        if (_view != null)
        {
            _view.SwapRequested -= OnSwapRequested;
            _view.CanStartSwap = null;
        }

        if (_moveCounter != null && _view != null)
            _moveCounter.OnMovesChanged -= OnMovesChangedForView;
    }

    /// <summary>Starts a fresh play of the level: builds its runtime through LevelFactory and draws the board.</summary>
    public void InitializeGame()
    {
        var level = LevelFactory.Create(_cfg, _view, this);
        _board = level.Board;
        _game = level.Game;
        _presenter = level.Presenter;

        _shownPoints = 0;
        _shownMatched = 0;
        _collected.Clear();

        _moveCounter.InitializeMoves(_cfg.maxMoves);
        _board.Initialize();

        _view.Build(_cfg.weidth, _cfg.height);
        _view.AssignSprites(_board);
        _view.SetLevelNumber(_cfg.levelIndex);
        _view.SetBackground(_cfg.BackgroundSprite);
        ApplySwapsEnabled();
    }

    // ---------------- Input ----------------

    /// <summary>Takes an input lock, for example while a booster plays its own animation. Dispose to release.</summary>
    public IDisposable AcquireInputLock(string reason) => _input.Acquire(reason);

    /// <summary>Stops swipes from committing without locking other input. Used while the bomb waits for a target.</summary>
    public void SetSwapsSuppressed(bool suppressed)
    {
        _swapsSuppressed = suppressed;
        ApplySwapsEnabled();
    }

    public bool IsBlackSheep(Animal piece) => piece != null && BlackSheep != null && piece._id == BlackSheep._id;

    private void ApplySwapsEnabled()
    {
        if (_view != null)
            _view.SwapsEnabled = _input.IsOpen && !_swapsSuppressed;
    }

    private bool CanStartSwapAt(Vector2Int coord)
    {
        if (_board == null || !_board.IsCellInBounds(coord))
            return false;

        var animal = _board.GetAnimalFromCell(coord);
        return animal != null && animal._canSwap;
    }

    public async void OnSwapRequested(Vector2Int from, Vector2Int to)
    {
        if (!CanAcceptInput) return;
        if (!IsTimerBombActive && _moveCounter.MovesLeft <= 0) return;

        if (IsTimerBombActive)
        {
            await TimerSwapAsync(from, to);
            return;
        }

        using (_input.Acquire("turn"))
        {
            var result = _game.TrySwap(from.ToGrid(), to.ToGrid(), CurrentContext());

            if (result.Outcome == TurnOutcome.Rejected)
            {
                if (result.RejectReason != RejectReason.EmptyCell)
                    await PlayInvalidSwapAsync(from, to);
                return;
            }

            if (!await _presenter.PlayAsync(result, destroyCancellationToken))
                return;

            if (result.Outcome == TurnOutcome.Resolved)
                await FinishTurnAsync(result, checkFailure: true);
        }
    }

    private async Task TimerSwapAsync(Vector2Int from, Vector2Int to)
    {
        var result = _game.SwapFree(from.ToGrid(), to.ToGrid());

        if (result.Outcome == TurnOutcome.Rejected)
        {
            if (result.RejectReason == RejectReason.NotSwappable)
                await PlayInvalidSwapAsync(from, to);
            return;
        }

        using (_input.Acquire("timer-swap"))
        {
            _timerSwapTask = _presenter.PlayAsync(result, destroyCancellationToken);
            await _timerSwapTask;
        }
    }

    private async Task PlayInvalidSwapAsync(Vector2Int from, Vector2Int to)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFX(SfxCannotSwap);

        await _view.AnimateInvalidSwap(from, to, to - from);
    }

    /// <summary>What happens after a turn has played out: win, lose, then any triggered dialogue.</summary>
    private async Task FinishTurnAsync(TurnResult result, bool checkFailure)
    {
        if (result.GoalsComplete)
            TryHandleLevelComplete();

        if (_animalDialogueController != null)
            _animalDialogueController.HideNormalBubbleIfActive();

        if (_isLevelOver)
            return;

        if (checkFailure && TryHandleLevelFailed())
            return;

        await TryShowTriggeredDialogueAsync();
    }

    private TurnContext CurrentContext() => new TurnContext(_moveCounter.MovesLeft, _cfg.maxMoves, IsTimerBombActive);

    // ---------------- Boosters ----------------

    /// <summary>Clears the bomb's area and plays out the cascade. The bomb plays its own impact animation first.</summary>
    public async Task ResolveBombAsync(IReadOnlyList<Vector2Int> cells)
    {
        if (_board == null || cells == null || cells.Count == 0)
            return;

        using (_input.Acquire("turn"))
        {
            var result = _game.ClearCells(cells.ToGrid(), CurrentContext());

            if (!await _presenter.PlayAsync(result, destroyCancellationToken))
                return;

            await FinishTurnAsync(result, checkFailure: false);
        }
    }

    public void StartTimerBomb(float durationSeconds)
    {
        if (_isLevelOver) return;

        IsTimerBombActive = true;
        _timerBombResolving = false;
        _timerBombEndTime = Time.time + durationSeconds;
        _lastShownTimerSecond = -1;

        OnTimerBombStateChanged?.Invoke(true);
        _view.SetTimerVisible(true);
        UpdateTimerUI();

        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlayTimerMusic();
            AudioManager.instance.PlaySFX(SfxTimerStart);
        }
    }

    private async void EndTimerBomb()
    {
        _timerBombResolving = true;
        IsTimerBombActive = false;

        OnTimerBombStateChanged?.Invoke(false);
        _view.SetTimerVisible(false);

        using (_input.Acquire("turn"))
        {
            await _timerSwapTask; // let the last free swap finish moving first

            if (AudioManager.instance != null)
                AudioManager.instance.PlayBG((int)_cfg.songNumber);

            var result = _game.ResolveBoard(CurrentContext());
            if (await _presenter.PlayAsync(result, destroyCancellationToken))
                await FinishTurnAsync(result, checkFailure: true);
        }

        _timerBombResolving = false;
    }

    private void UpdateTimerUI()
    {
        float remaining = Mathf.Max(0f, _timerBombEndTime - Time.time);
        int seconds = Mathf.CeilToInt(remaining);

        if (seconds == _lastShownTimerSecond)
            return;

        _lastShownTimerSecond = seconds;
        _view.SetTimerSeconds(seconds);
    }

    // ---------------- Hint and dead board ----------------

    public async Task ShowHintOrHandleDeadBoardAsync()
    {
        if (!CanAcceptInput)
            return;

        if (_hintFinder.TryFindHint(_board, out HintMove hint))
            await _view.AnimateHint(hint.From, hint.To);
        else
            await EnsurePlayableBoardAsync();
    }

    private async Task EnsurePlayableBoardAsync()
    {
        if (_isLevelOver || _game == null) return;

        var result = _game.EnsurePlayable(CurrentContext());
        if (result.Steps.Count == 0) return;

        using (_input.Acquire("shuffle"))
            await _presenter.PlayAsync(result, destroyCancellationToken);
    }

    // ---------------- Presentation callbacks (ITurnPresentationListener) ----------------

    void ITurnPresentationListener.OnMoveUsed() => _moveCounter.UseMove();

    void ITurnPresentationListener.OnTilesScored(int points, int matched)
    {
        _shownPoints += points;
        _shownMatched += matched;

        if (points > 0 && _levelScoreEventChannelSO != null)
            _levelScoreEventChannelSO.RaiseEvent(points);
    }

    void ITurnPresentationListener.OnTilesDestroyed(string tileId, int count)
    {
        if (string.IsNullOrEmpty(tileId) || count <= 0)
            return;

        if (_animalsDestroyedChannelSO != null)
            _animalsDestroyedChannelSO.RaiseEvent(tileId, count);

        _collected.TryGetValue(tileId, out int have);
        _collected[tileId] = have + count;

        if (HasCollectGoals)
            UpdateGoalUI();
    }

    void ITurnPresentationListener.OnBoneBroken(Vector2Int cell)
    {
        _view.PlayBoneBreakFx(cell);
        if (AudioManager.instance != null)
            AudioManager.instance.PlayBoneBreak();
    }

    void ITurnPresentationListener.OnGoalProgressChanged() => UpdateGoalUI();

    private void OnMovesChangedForView(int movesLeft)
    {
        if (_view == null)
            return;

        _view.SetMovesText(movesLeft);
        _view.SetMovesLastMoveTension(movesLeft == 1);
    }

    private void UpdateGoalUI()
    {
        if (_board == null)
            return;

        // Points plus collect goals (last-level style)
        if (_cfg.goalType == PointsOrMatches.points && HasCollectGoals)
        {
            _view.SetPointsAndCollectGoals(_shownPoints, _cfg.goal, _cfg.collectGoals, _collected);
            return;
        }

        if (_cfg.goalType == PointsOrMatches.collectAnimals)
            _view.SetCollectGoals(_cfg.collectGoals, _collected);

        if (_board.GoalType == PointsOrMatches.points)
            _view.SetScore(_shownPoints, _board.GoalAmount);
        else if (_board.GoalType == PointsOrMatches.matches)
            _view.SetMatchedAnimals(_shownMatched, _board.GoalAmount);

        _view.ShowGoal(true);
    }

    /// <summary>Makes the shown progress match the core, for example after a debug cheat.</summary>
    private void SyncShownProgress()
    {
        var session = _board.Session;
        _shownPoints = session.Points;
        _shownMatched = session.MatchedAnimals;
        _collected.Clear();
        foreach (var entry in session.Collected)
            _collected[entry.Key] = entry.Value;
    }

    // ---------------- Win / lose ----------------

    private void EndLevel()
    {
        _isLevelOver = true;
        if (_levelOverLock == null)
            _levelOverLock = _input.Acquire("level-over");
    }

    private bool TryHandleLevelComplete()
    {
        if (_isLevelOver || !_board.Session.AreAllGoalsComplete)
            return false;

        EndLevel();

        if (_levelVFXToggle != null)
            _levelVFXToggle.SetCurrentVFXActive(false);

        _levelCompletedChannelSO?.RaiseEvent(_cfg.levelIndex);

        int movesUsed = _moveCounter.MovesUsed;
        int stars = _rewards.GetStars(_cfg.maxMoves, movesUsed);
        int coins = _rewards.GetCoins(stars, _cfg.levelIndex);
        int finalScore = _board.CurrentPoints;
        int level = _cfg.levelIndex;

        if (_locator != null && _locator.Bootstrapper != null)
        {
            _locator.Bootstrapper.Economy.AddCoins(coins);

            var state = _locator.Bootstrapper.Economy.State;

            // Save best star count per level
            state.levelStars.TryGetValue(level, out int bestStars);
            if (stars > bestStars)
                state.levelStars[level] = stars;

            // Save best score per level and recalculate total (match the displayed score formula)
            int displayScore = finalScore * 10;
            int shownScore = Mathf.RoundToInt(displayScore * (stars / 3f));
            state.TrySetLevelBestScore(level, shownScore);
            _locator.Bootstrapper.Economy.Save();

            // Submit cumulative best to leaderboard
            var leaderboard = _locator.Bootstrapper.Leaderboard;
            if (leaderboard != null && leaderboard.IsReady)
            {
                string displayName = string.IsNullOrEmpty(state.playerName) ? "Player" : state.playerName;
                _ = leaderboard.AddScore(state.playerId, displayName, state.totalPointsEarned);
            }
        }

        if (_levelClearedDimmer != null)
            _levelClearedDimmer.SetActive(true);

        if (_levelClearedPopupUI != null)
            _levelClearedPopupUI.Show(level, finalScore, coins, stars);
        else
            Debug.LogError("LevelClearedPopupUI is not assigned on BoardController.");

        if (AudioManager.instance != null)
        {
            AudioManager.instance.ChangeMusicVolume(0.4f);
            AudioManager.instance.PlaySFX(SfxLevelCleared);
            AudioManager.instance.ChangeMusicVolume(1f);
        }

        return true;
    }

    private bool TryHandleLevelFailed()
    {
        if (_isLevelOver) return true;
        if (_board.Session.AreAllGoalsComplete) return false;
        if (_moveCounter.MovesLeft > 0) return false;

        EndLevel();

        if (_levelVFXToggle != null)
            _levelVFXToggle.SetCurrentVFXActive(false);

        if (_locator != null && _locator.Bootstrapper != null)
            _locator.Bootstrapper.Economy.TrySpendLifeOnLevelFail();

        if (_levelLostPopupUI != null)
            _levelLostPopupUI.Show();
        else
            Debug.LogError("LevelLostPopupUI is not assigned on BoardController.");

        return true;
    }

    // ---------------- Dialogue ----------------

    private async Task TryStartLevelDialogueAsync()
    {
        if (_isLevelOver || _animalDialogueController == null)
            return;

        var context = new BoardDialogueContext(_board, _view, PlayAnimalSpeakSfx);
        await _animalDialogueController.HandleLevelStartBubblesAsync(_cfg.levelIndex, context, _cfg.weidth);
    }

    private async Task TryShowTriggeredDialogueAsync()
    {
        if (_animalDialogueController == null)
            return;

        var context = new BoardDialogueContext(_board, _view, PlayAnimalSpeakSfx);
        await _animalDialogueController.TryShowTriggeredBubbleAsync(_cfg.levelIndex, context, _cfg.weidth);
    }

    private async Task PlayAnimalSpeakSfx(Animal animal)
    {
        if (animal == null || animal._speakSfxId == 0) return;
        if (AudioManager.instance == null) return;

        AudioManager.instance.PlaySFX(animal._speakSfxId);

        if (_speakSfxDuration > 0f)
            await Task.Delay(Mathf.CeilToInt(_speakSfxDuration * 1000f));
    }

    // ---------------- Debug ----------------

    public void DebugForceWin()
    {
        if (_isLevelOver || _board == null)
            return;

        _board.Session.CheatCompleteGoals();
        SyncShownProgress();
        UpdateGoalUI();
        TryHandleLevelComplete();
    }

    public void DebugForceLose()
    {
        if (_isLevelOver)
            return;

        EndLevel();
        IsTimerBombActive = false;
        _view.SetTimerVisible(false);

        if (_locator != null && _locator.Bootstrapper != null)
            _locator.Bootstrapper.Economy.TrySpendLifeOnLevelFail();

        if (_levelVFXToggle != null)
            _levelVFXToggle.SetCurrentVFXActive(false);

        if (_levelLostPopupUI != null)
            _levelLostPopupUI.Show();
    }

    public void DebugPlaceAnimalAtCenter(Animal animal, Animal blackSheep, Animal boneBlock)
    {
        if (!Application.isPlaying || _board == null || _view == null || !CanAcceptInput || !_view.SwapsEnabled)
        {
            Debug.LogWarning("Wait until the board is ready and idle before placing a debug tile.", this);
            return;
        }

        if (animal == null)
        {
            Debug.LogWarning("Assign the requested tile on LevelDebugFlowCheats.", this);
            return;
        }

        _debugBlackSheep = blackSheep;
        _debugBoneBlock = boneBlock;
        _board.DebugSetSpecialAnimals(BlackSheep, BoneBlock);

        var center = new Vector2Int(_board.Width / 2, _board.Height / 2);
        _board.SetAnimalInCell(center, animal);
        _view.AssignSprites(_board);
    }
}
