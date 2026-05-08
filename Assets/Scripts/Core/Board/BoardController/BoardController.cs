using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;

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
    [SerializeField] private ScoreEventChannelSO _scoreEventChannelSO;

    [SerializeField] private LevelScoreEventChannelSO _levelScoreEventChannelSO;

    [Header("Speech Bubble Configs")]
    [SerializeField] private AnimalSpeechConfig _speechConfig;
    [SerializeField] private SpeechBubblePresenter _speechBubblePresenter;
    //[SerializeField] private float _normalBubbleDelaySeconds = 2.5f;
    //[SerializeField] private float _normalBubbleVisibleSeconds = 3f;
    //[SerializeField] private float _triggeredBubbleVisibleSeconds = 3f;
    //[SerializeField] private float _speechCellHighlightDuration = 0.48f;
    [SerializeField] private AnimalDialogueController _animalDialogueController;

    public BoardConfig Config => _cfg;

    /// <summary>True while tutorial sequence runs or a triggered bubble is showing (normal bubble does not set this).</summary>
    public bool IsSpeechBubbleInputBlocked => _bubbleActive;

    private Board _board;
    private bool _isBusy; // If an animation is going, or in the middle of a swap/cascade 
    private bool _isLevelOver = false;
    private readonly Queue<(Vector2Int wolf, Vector2Int sheep)> _pendingWolfEats = new(); // Shich wolf ate which sheep

    // Timer bomb parameters
    private readonly BoardControllerTimerBombState _timerBomb = new BoardControllerTimerBombState();
    public bool IsTimerBombActive => _timerBomb.IsActive;
    public float _speakSfxDuration = 1f;

    public Action<bool> OnTimerBombStateChanged;

    public int GetWidth() => _cfg.weidth;
    public int GetHeight() => _cfg.height;
    public Board CurrentBoard => _board;

    private BoardControllerGoalTracker _goalTracker;
    private readonly BoardControllerBlackSheepBlast _blackSheepBlastAnimator = new BoardControllerBlackSheepBlast();

    // For the out of moves shuffle
    private readonly BoardControllerPlayableBoardEnsurer _playableBoardEnsurer = new BoardControllerPlayableBoardEnsurer();
    private bool _bubbleActive;
    private readonly HashSet<int> _triggeredEntryIndicesShown = new HashSet<int>();

    private BoardControllerSwapFlow _swapFlow;
    private BoardControllerCascadeFlow _cascadeFlow;
    private BoardControllerLevelResultHandler _levelResultHandler;
    private BoardControllerTimerBombFlow _timerBombController;
    private BoardControllerGameInitializer _gameInitializer;
    private BoardControllerDialogueFlow _dialogueFlow;

    private BoardControllerSwapFlow SwapFlow => _swapFlow ??= new BoardControllerSwapFlow(this);
    private BoardControllerCascadeFlow CascadeFlow => _cascadeFlow ??= new BoardControllerCascadeFlow(this);
    private BoardControllerLevelResultHandler LevelResultHandler => _levelResultHandler ??= new BoardControllerLevelResultHandler(this);
    private BoardControllerTimerBombFlow TimerBombController => _timerBombController ??= new BoardControllerTimerBombFlow(this);
    private BoardControllerGameInitializer GameInitializer => _gameInitializer ??= new BoardControllerGameInitializer(this);
    private BoardControllerDialogueFlow DialogueFlow => _dialogueFlow ??= new BoardControllerDialogueFlow(this);

    internal BoardConfig Cfg => _cfg;
    internal BoardView View => _view;
    internal MoveCounter MoveCounter => _moveCounter;
    internal Board Board { get => _board; set => _board = value; }
    internal BoardControllerTimerBombState TimerBomb => _timerBomb;
    internal Queue<(Vector2Int wolf, Vector2Int sheep)> PendingWolfEats => _pendingWolfEats;
    internal bool IsBusy { get => _isBusy; set => _isBusy = value; }
    internal bool IsLevelOver { get => _isLevelOver; set => _isLevelOver = value; }
    internal LevelClearedPopupUI LevelClearedPopupUI => _levelClearedPopupUI;
    internal GameObject LevelClearedDimmer => _levelClearedDimmer;
    internal LevelLostPopupUI LevelLostPopupUI => _levelLostPopupUI;
    internal LevelVFXToggle LevelVFXToggle => _levelVFXToggle;
    internal RewardsConfig Rewards => _rewards;
    internal BootstrapperLocator Locator => _locator;
    internal LevelCompletedEventChannelSO LevelCompletedChannelSO => _levelCompletedChannelSO;
    internal AnimalsDestroyedEventChannelSO AnimalsDestroyedChannelSO => _animalsDestroyedChannelSO;
    internal LevelScoreEventChannelSO LevelScoreEventChannelSO => _levelScoreEventChannelSO;
    internal HashSet<int> TriggeredEntryIndicesShown => _triggeredEntryIndicesShown;
    internal BoardControllerGoalTracker GoalTracker { get => _goalTracker; set => _goalTracker = value; }
    internal AnimalDialogueController AnimalDialogueController => _animalDialogueController;
    internal float SpeakSfxDuration => _speakSfxDuration;

    private void Awake()
    {
        var bootstrapper = GameBootstrapper.Instance;
        if (bootstrapper != null && bootstrapper.SelectedLevel != null)
            _cfg = bootstrapper.SelectedLevel;
    }

    public async void Start()
    {
        if (_cfg == null || _view == null)
            Debug.LogError("Error: Either cfg or view weren't inserted in the board controller");

        InitializeGame();

        // Show initial goal/progress
        UpdateGoalUI();

        await EnsurePlayableBoardAsync();
        await TryStartLevelDialogueAsync();
    }

    private void Update()
    {
        TimerBombController.Tick();
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

        _timerBomb.ResetLastShownSecond();
        if (_view != null)
        {
            _view.SetTimerVisible(IsTimerBombActive);
            if (IsTimerBombActive)
                _timerBomb.UpdateTimerUI(_view);
        }

        if (AudioManager.instance != null && _cfg != null)
        {
            AudioManager.instance.PlayBG((int)_cfg.songNumber);
        }
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

    private void OnMovesChangedForView(int movesLeft)
    {
        if (_view == null)
            return;

        _view.SetMovesText(movesLeft);
        _view.SetMovesLastMoveTension(movesLeft == 1);
    }

    public void DebugForceWin()
    {
        if (_isLevelOver || _board == null)
            return;

        _board.DebugCheatFillPrimaryGoal();

        _goalTracker.DebugCompleteCollectGoals();

        UpdateGoalUI();
        TryHandleLevelComplete();
    }

    public void DebugForceLose()
    {
        LevelResultHandler.DebugForceLose();
    }

    public void InitializeGame()
    {
        GameInitializer.InitializeGame();
    }

    public async void OnSwapRequested(Vector2Int from, Vector2Int to)
    {
        await SwapFlow.HandleSwapRequestedAsync(from, to);
    }

    internal void TryRollBlackSheep()
    {
        if (_cfg.blackSheep == null) return; // not this level

        int movesMade = _cfg.maxMoves - _moveCounter.MovesLeft;

        if (_cfg.blackSheepUnlockAfterMoves <= 0) return;

        if (movesMade % _cfg.blackSheepUnlockAfterMoves == 0)
        {
            _board.RollForBlackSheep(_cfg.blackSheepRollChance);
        }
    }

    public async void TryRemoveCellsFromGrid(List<Vector2Int> cells)
    {
        await CascadeFlow.RemoveCellsFromGridAsync(cells);
    }

    public void StartTimerBomb(float durationSeconds)
    {
        TimerBombController.StartTimerBomb(durationSeconds);
    }

    internal Task ResolveCascadesAsync()
    {
        return CascadeFlow.ResolveCascadesAsync();
    }

    internal bool TryHandleLevelComplete()
    {
        return LevelResultHandler.TryHandleLevelComplete();
    }

    internal bool TryHandleLevelFailed()
    {
        return LevelResultHandler.TryHandleLevelFailed();
    }

    internal void UpdateGoalUI()
    {
        _goalTracker.UpdateGoalUI(_board);
    }

    public async Task ShowHintOrHandleDeadBoardAsync()
    {
        if (_isBusy || _isLevelOver || _board == null)
            return;

        if (_playableBoardEnsurer.TryFindHint(_board, out _))
        {
            await _playableBoardEnsurer.ShowHintAsync(_board, _view);
        }
        else
        {
            await EnsurePlayableBoardAsync();
        }
    }

    internal async Task EnsurePlayableBoardAsync()
    {
        if (_isLevelOver) return;
        if (!IsTimerBombActive && _moveCounter.MovesLeft <= 0) return;

        if (_playableBoardEnsurer.TryFindHint(_board, out _))
            return;

        _isBusy = true;
        await _playableBoardEnsurer.ShuffleUntilPlayableAsync(_board, _view);
        _isBusy = false;
    }

    //private async Task WaitFrames(int frameCount)
    //{
    //    for (int i = 0; i < frameCount; i++)
    //        await Task.Yield();
    //}

    internal bool AreAllGoalsComplete()
    {
        return _goalTracker.AreAllGoalsComplete(_board);
    }

    private bool CanStartSwapAt(Vector2Int coord)
    {
        if (_board == null)
            return false;

        if (coord.x < 0 || coord.x >= GetWidth() || coord.y < 0 || coord.y >= GetHeight())
            return false;

        if (_board.IsCellLocked(coord))
            return false;

        var animal = _board.GetAnimalFromCell(coord);
        return animal != null && animal._canSwap;
    }

    private bool IsAnimal(Animal piece, Animal target)
    {
        return piece != null && target != null && piece._id == target._id;
    }

    internal bool IsAnySheep(Animal piece)
    {
        return IsAnimal(piece, _cfg.blackSheep);
    }


    internal Task AnimateBlackSheepBlastFromCenter(Vector2Int center, bool swipedVertically)
    {
        return _blackSheepBlastAnimator.AnimateFromCenter(_board, _view, _cfg, center, swipedVertically);
    }

    internal void HideNormalBubbleIfActive()
    {
        _animalDialogueController.HideNormalBubbleIfActive();
    }

    private Task TryStartLevelDialogueAsync()
    {
        return DialogueFlow.TryStartLevelDialogueAsync();
    }

    internal Task TryShowTriggeredDialogueAsync()
    {
        return DialogueFlow.TryShowTriggeredDialogueAsync();
    }
}
