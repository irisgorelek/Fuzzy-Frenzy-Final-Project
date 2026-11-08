using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// The level scene's entry point. It holds the scene references, builds a fresh level on start, and passes
/// input and booster requests on. The work itself lives in plain classes: <see cref="TurnRunner"/> plays turns,
/// <see cref="LevelProgress"/> tracks goals and ends the level, <see cref="TimerBoosterClock"/> keeps the
/// timer booster's time and <see cref="LevelDialogue"/> shows speech bubbles. All input goes through one
/// <see cref="InputGate"/>.
/// </summary>
public class BoardController : MonoBehaviour
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

    private readonly InputGate _input = new InputGate();
    private readonly IGameAudio _audio = AudioManagerAudio.Instance;

    private Board _board;
    private LevelProgress _progress;
    private LevelDialogue _dialogue;
    private TurnRunner _turns;
    private TimerBoosterClock _timer;
    private LevelOutcomePresenter _outcome;

    private bool _swapsSuppressed;

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

    private bool IsLevelOver => _progress != null && _progress.IsOver;

    // ---------------- Lifecycle ----------------

    private void Awake()
    {
        _input.Changed += _ => ApplySwapsEnabled();

        var bootstrapper = GameBootstrapper.Instance;
        if (bootstrapper != null && bootstrapper.SelectedLevel != null)
            _cfg = bootstrapper.SelectedLevel;

        if (_view != null)
            _view.Audio = _audio;

        _timer = new TimerBoosterClock(_view, _audio);
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
            Audio = _audio,
        });
    }

    public void Start() => StartLevelAsync().Forget(this);

    private async Task StartLevelAsync()
    {
        if (_cfg == null || _view == null)
            Debug.LogError("Error: Either cfg or view weren't inserted in the board controller");

        InitializeGame();

        await _turns.EnsurePlayableAsync();
        if (!IsLevelOver)
            await _dialogue.PlayLevelStartAsync(_board, _view);
    }

    private void Update()
    {
        if (!IsTimerBombActive || _turns.IsResolvingTimer)
            return;

        if (_timer.Tick() || _board.Session.AreAllGoalsComplete)
            _turns.EndTimerAsync().Forget(this);
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

        if (_cfg != null)
            _audio.PlayLevelMusic(_cfg.songNumber);
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
        _progress?.Dispose();

        _board = LevelFactory.CreateBoard(_cfg);
        _progress = new LevelProgress(_cfg, _board.Session, _moveCounter, _input, _view, _outcome,
            _levelScoreEventChannelSO, _animalsDestroyedChannelSO);
        _dialogue = new LevelDialogue(_animalDialogueController, _cfg, _speakSfxDuration, _audio);

        var level = LevelFactory.Create(_board, _cfg, _view, _progress, _audio);
        _turns = new TurnRunner(level, _progress, _dialogue, _timer, _moveCounter, _input, _view, _audio, _cfg, destroyCancellationToken);

        _moveCounter.InitializeMoves(_cfg.maxMoves);
        _board.Initialize();

        _view.Build(_cfg.weidth, _cfg.height);
        _view.AssignSprites(_board);
        _view.SetLevelNumber(_cfg.levelIndex);
        _view.SetBackground(_cfg.BackgroundSprite);
        _progress.Render();
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

    public void OnSwapRequested(Vector2Int from, Vector2Int to)
    {
        if (CanAcceptInput)
            _turns.SwapAsync(from, to).Forget(this);
    }

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

    private void OnMovesChangedForView(int movesLeft)
    {
        if (_view == null)
            return;

        _view.SetMovesText(movesLeft);
        _view.SetMovesLastMoveTension(movesLeft == 1);
    }

    // ---------------- Boosters and hints ----------------

    /// <summary>Clears the bomb's area and plays out the cascade. The bomb plays its own impact animation first.</summary>
    public Task ResolveBombAsync(IReadOnlyList<Vector2Int> cells) =>
        _board == null ? Task.CompletedTask : _turns.ClearCellsAsync(cells);

    public void StartTimerBomb(float durationSeconds) => _turns.StartTimer(durationSeconds);

    public Task ShowHintOrHandleDeadBoardAsync() =>
        CanAcceptInput ? _turns.ShowHintOrShuffleAsync() : Task.CompletedTask;

    // ---------------- Debug ----------------

    public void DebugForceWin()
    {
        if (_board != null)
            _progress.ForceWin();
    }

    public void DebugForceLose()
    {
        if (_progress != null && _progress.ForceLose())
            _timer.Stop();
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
