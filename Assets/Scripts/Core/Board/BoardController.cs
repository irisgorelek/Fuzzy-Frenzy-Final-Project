using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;
using UnityEngine;

/// <summary>
/// Unity entry point for a level. Turns input into calls on <see cref="Match3Game"/> and replays each
/// <see cref="TurnResult"/> through <see cref="TurnPresenter"/>. Everything around a turn is delegated:
/// goal progress (<see cref="GoalProgressTracker"/>), the end of the level (<see cref="LevelFlow"/>,
/// <see cref="LevelOutcomePresenter"/>), the timer booster (<see cref="TimerBoosterClock"/>) and dialogue
/// (<see cref="LevelDialogue"/>). All input goes through one <see cref="InputGate"/>.
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

    private const int SfxCannotSwap = 4;

    private readonly InputGate _input = new InputGate();
    private readonly BoardHintFinder _hintFinder = new BoardHintFinder();

    private Board _board;
    private Match3Game _game;
    private TurnPresenter _presenter;

    private LevelFlow _flow;
    private GoalProgressTracker _goals;
    private LevelOutcomePresenter _outcome;
    private LevelDialogue _dialogue;
    private TimerBoosterClock _timer;

    private bool _swapsSuppressed;
    private bool _timerResolving;
    private Task _timerSwapTask = Task.CompletedTask;

    public Action<bool> OnTimerBombStateChanged;

    public BoardConfig Config => _cfg;
    public Board CurrentBoard => _board;
    public bool IsTimerBombActive => _timer != null && _timer.IsActive;

    /// <summary>True while a tutorial or a triggered bubble is showing.</summary>
    public bool IsSpeechBubbleInputBlocked => _animalDialogueController != null && _animalDialogueController.IsBlockingInput;

    /// <summary>True while a turn, booster or shuffle is playing out.</summary>
    public bool IsBusy => !_input.IsOpen;

    /// <summary>Whether a swap, booster or hint may start right now.</summary>
    public bool CanAcceptInput => _board != null && _input.IsOpen && !IsLevelOver && !IsSpeechBubbleInputBlocked;

    public int GetWidth() => _cfg.weidth;
    public int GetHeight() => _cfg.height;

    private bool IsLevelOver => _flow != null && _flow.IsOver;

    // ---------------- Lifecycle ----------------

    private void Awake()
    {
        _input.Changed += _ => ApplySwapsEnabled();

        var bootstrapper = GameBootstrapper.Instance;
        if (bootstrapper != null && bootstrapper.SelectedLevel != null)
            _cfg = bootstrapper.SelectedLevel;

        _timer = new TimerBoosterClock(_view);
        _timer.ActiveChanged += active => OnTimerBombStateChanged?.Invoke(active);

        _outcome = new LevelOutcomePresenter(new LevelOutcomeServices
        {
            ClearedPopup = _levelClearedPopupUI,
            ClearedDimmer = _levelClearedDimmer,
            LostPopup = _levelLostPopupUI,
            VfxToggle = _levelVFXToggle,
            Rewards = _rewards,
            Locator = _locator,
            LevelCompletedChannel = _levelCompletedChannelSO,
        });
    }

    public async void Start()
    {
        if (_cfg == null || _view == null)
            Debug.LogError("Error: Either cfg or view weren't inserted in the board controller");

        InitializeGame();
        _goals.Render();

        await EnsurePlayableBoardAsync();
        if (!IsLevelOver)
            await _dialogue.PlayLevelStartAsync(_board, _view);
    }

    private void Update()
    {
        if (!IsTimerBombActive || _timerResolving)
            return;

        if (_timer.Tick() || _board.Session.AreAllGoalsComplete)
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

        if (_view != null)
            _timer.SyncDisplay();

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

        _flow?.Dispose();
        _flow = new LevelFlow(_input);
        _goals = new GoalProgressTracker(_cfg, _view);
        _dialogue = new LevelDialogue(_animalDialogueController, _cfg, _speakSfxDuration);

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

    /// <summary>True for a tile that blasts a line when swapped (the black sheep).</summary>
    public static bool IsBlackSheep(Animal piece) => piece != null && (piece._traits & TileTraits.LineBlastOnSwap) != 0;

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
            TryWin();

        _dialogue.HideNormalBubble();

        if (IsLevelOver)
            return;

        if (checkFailure && TryLose())
            return;

        await _dialogue.PlayTriggeredAsync(_board, _view);
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
        if (IsLevelOver) return;

        _timerResolving = false;
        _timer.Start(durationSeconds);
    }

    private async void EndTimerBomb()
    {
        _timerResolving = true;
        _timer.Stop();

        using (_input.Acquire("turn"))
        {
            await _timerSwapTask; // let the last free swap finish moving first

            if (AudioManager.instance != null)
                AudioManager.instance.PlayBG((int)_cfg.songNumber);

            var result = _game.ResolveBoard(CurrentContext());
            if (await _presenter.PlayAsync(result, destroyCancellationToken))
                await FinishTurnAsync(result, checkFailure: true);
        }

        _timerResolving = false;
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
        if (IsLevelOver || _game == null) return;

        var result = _game.EnsurePlayable(CurrentContext());
        if (result.Steps.Count == 0) return;

        using (_input.Acquire("shuffle"))
            await _presenter.PlayAsync(result, destroyCancellationToken);
    }

    // ---------------- Presentation callbacks (ITurnPresentationListener) ----------------

    void ITurnPresentationListener.OnMoveUsed() => _moveCounter.UseMove();

    void ITurnPresentationListener.OnTilesScored(int points, int matched)
    {
        _goals.AddScored(points, matched);

        if (points > 0 && _levelScoreEventChannelSO != null)
            _levelScoreEventChannelSO.RaiseEvent(points);
    }

    void ITurnPresentationListener.OnTilesDestroyed(string tileId, int count)
    {
        if (string.IsNullOrEmpty(tileId) || count <= 0)
            return;

        if (_animalsDestroyedChannelSO != null)
            _animalsDestroyedChannelSO.RaiseEvent(tileId, count);

        _goals.AddDestroyed(tileId, count);

        if (_goals.HasCollectGoals)
            _goals.Render();
    }

    void ITurnPresentationListener.OnBoneBroken(Vector2Int cell)
    {
        _view.PlayBoneBreakFx(cell);
        if (AudioManager.instance != null)
            AudioManager.instance.PlayBoneBreak();
    }

    void ITurnPresentationListener.OnGoalProgressChanged() => _goals.Render();

    private void OnMovesChangedForView(int movesLeft)
    {
        if (_view == null)
            return;

        _view.SetMovesText(movesLeft);
        _view.SetMovesLastMoveTension(movesLeft == 1);
    }

    // ---------------- Win / lose ----------------

    private bool TryWin()
    {
        if (!_board.Session.AreAllGoalsComplete || !_flow.TryEnd(LevelPhase.Won))
            return false;

        _outcome.ShowWin(_cfg, _moveCounter.MovesUsed, _board.CurrentPoints);
        return true;
    }

    /// <summary>Ends the level as lost if the moves ran out with goals still open. True if the level is over.</summary>
    private bool TryLose()
    {
        if (IsLevelOver) return true;
        if (_board.Session.AreAllGoalsComplete) return false;
        if (_moveCounter.MovesLeft > 0) return false;

        _flow.TryEnd(LevelPhase.Lost);
        _outcome.ShowLose();
        return true;
    }

    // ---------------- Debug ----------------

    public void DebugForceWin()
    {
        if (IsLevelOver || _board == null)
            return;

        _board.Session.CheatCompleteGoals();
        _goals.SyncWith(_board.Session);
        _goals.Render();
        TryWin();
    }

    public void DebugForceLose()
    {
        if (_flow == null || !_flow.TryEnd(LevelPhase.Lost))
            return;

        _timer.Stop();
        _outcome.ShowLose();
    }

    public void DebugPlaceAnimalAtCenter(Animal animal)
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

        var center = new Vector2Int(_board.Width / 2, _board.Height / 2);
        _board.SetAnimalInCell(center, animal);
        _view.AssignSprites(_board);
    }
}
