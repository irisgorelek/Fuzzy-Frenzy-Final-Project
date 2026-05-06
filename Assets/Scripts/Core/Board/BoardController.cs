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
    //[SerializeField] private GameObject _levelLostPopup;
    [SerializeField] private GameObject _levelClearedDimmer;
    [SerializeField] private LevelLostPopupUI _levelLostPopupUI;
    //[SerializeField] private int framesBetweenSteps = 5;
    [SerializeField] private LevelVFXToggle _levelVFXToggle;

    [Header("Rewards Configs")]
    [SerializeField] private RewardsConfig _rewards;
    [SerializeField] private BootstrapperLocator _locator; // to add coins

    [SerializeField] private LevelCompletedEventChannelSO _levelCompletedChannelSO;
    [SerializeField] private AnimalsDestroyedEventChannelSO _animalsDestroyedChannelSO;
    [SerializeField] private ScoreEventChannelSO _scoreEventChannelSO;

    [SerializeField] private LevelScoreEventChannelSO _levelScoreEventChannelSO;

    [Header("Speech Bubble Configs")]
    [SerializeField] private AnimalSpeechConfig _speechConfig;
    [SerializeField] private SpeechBubblePresenter _speechBubblePresenter;
    [SerializeField] private float _normalBubbleDelaySeconds = 2.5f;
    [SerializeField] private float _normalBubbleVisibleSeconds = 3f;
    [SerializeField] private float _triggeredBubbleVisibleSeconds = 3f;
    [SerializeField] private float _speechCellHighlightDuration = 0.48f;
    [SerializeField] private AnimalDialogueController _animalDialogueController;

    public BoardConfig Config => _cfg;

    /// <summary>True while tutorial sequence runs or a triggered bubble is showing (normal bubble does not set this).</summary>
    public bool IsSpeechBubbleInputBlocked => _bubbleActive;

    private Board _board;
    private bool _isBusy; // If an animation is going, or in the middle of a swap/cascade 
    private bool _isLevelOver = false;
    private readonly Queue<(Vector2Int wolf, Vector2Int sheep)> _pendingWolfEats = new(); // Shich wolf ate which sheep

    private Dictionary<string, int> _collected = new Dictionary<string, int>(); // Track collected animals

    // Timer bomb parameters
    public bool IsTimerBombActive { get; private set; }
    private float _timerBombEndTime;
    private bool _timerBombResolving;
    private int _lastShownTimerSecond = -1;
    public float _speakSfxDuration = 1f;

    public Action<bool> OnTimerBombStateChanged;

    public int GetWidth() => _cfg.weidth;
    public int GetHeight() => _cfg.height;
    public Board CurrentBoard => _board;
    private bool HasCollectGoals => _cfg.collectGoals != null && _cfg.collectGoals.Count > 0;

    // For the out of moves shuffle
    private readonly BoardHintFinder _hintFinder = new BoardHintFinder();
    private bool _bubbleActive;
    private readonly HashSet<int> _triggeredEntryIndicesShown = new HashSet<int>();

    private void Awake()
    {
        //_levelClearedPopup.gameObject.SetActive(false);
        //_levelLostPopup.gameObject.SetActive(false);

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
        await HandleLevelStartBubblesAsync();
    }

    private void Update()
    {
        if (!IsTimerBombActive || _timerBombResolving)
        {
            return;
        }

        UpdateTimerUI();

        if (Time.time >= _timerBombEndTime || AreAllGoalsComplete())
        {
            EndTimerBomb();
        }
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
        _view.SetTimerVisible(IsTimerBombActive);
        if (IsTimerBombActive)
            UpdateTimerUI();

        if (AudioManager.instance != null)
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

        if (HasCollectGoals && _cfg.collectGoals != null)
        {
            foreach (var g in _cfg.collectGoals)
            {
                if (g.animal == null || string.IsNullOrEmpty(g.animal._id))
                    continue;
                _collected[g.animal._id] = g.amount;
            }
        }

        UpdateGoalUI();
        TryHandleLevelComplete();
    }

    public void DebugForceLose()
    {
        if (_isLevelOver)
            return;

        _isLevelOver = true;
        _view.SwapsEnabled = false;
        IsTimerBombActive = false;
        _view.SetTimerVisible(false);

        if (_locator != null && _locator.Bootstrapper != null)
            _locator.Bootstrapper.Economy.TrySpendLifeOnLevelFail();
        
        if (_levelVFXToggle != null)
            _levelVFXToggle.SetCurrentVFXActive(false);

        if (_levelLostPopupUI != null)
            _levelLostPopupUI.Show();
    }

    public void InitializeGame()
    {
        // Technical
        _board = new Board(_cfg);
        _triggeredEntryIndicesShown.Clear();

        _board.OnWolfAteSheep += (wolf, sheep) =>
        {
            _pendingWolfEats.Enqueue((wolf, sheep));
        };

        _collected.Clear();
        _board.OnAnimalsDestroyed = HandleAnimalsDestroyed;

        //_board.OnScoreAdded = amount => _scoreEventChannelSO.RaiseEvent(amount);
        _board.OnScoreAdded += amount => _levelScoreEventChannelSO.RaiseEvent(amount); // In-Level

        _moveCounter.InitializeMoves(_cfg.maxMoves);

        _board.Initialize();

        //_board.InitializeStaticDeadBoard(); // For the shuffle tests

        //_blackSheepTriggered = false;

        // Visual
        _view.Build(_cfg.weidth, _cfg.height);
        _view.AssignSprites(_board);
        _view.SetLevelNumber(_cfg.levelIndex);
        _view.SetBackground(_cfg.BackgroundSprite);
    }

    public async void OnSwapRequested(Vector2Int from, Vector2Int to)
    {
        if (_isBusy || _isLevelOver || _bubbleActive) return;
        if (!IsTimerBombActive && _moveCounter.MovesLeft <= 0) return;

        var a = _board.GetAnimalFromCell(from);
        var b = _board.GetAnimalFromCell(to);

        if (a == null || b == null) 
            return;

        if (!a._canSwap || !b._canSwap)
        {
            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(4); // can't swap sound

            await _view.AnimateInvalidSwap(from, to, to - from);
            return;
        }

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
            await TryShowTriggeredBubbleAsync();
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
            _isBusy = false;

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFX(4);

            await _view.AnimateInvalidSwap(from, to, to - from);
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

            // Animate the blast first, using the same pop FX as normal matches
            await AnimateBlackSheepBlastFromCenter(sheepPosAfterSwap, swipeVertical);

            // Then actually clear the model and show the result
            _board.TriggerSheepSwipeBlast(sheepPosAfterSwap, swipeVertical);

            UpdateGoalUI();
            _view.AssignSprites(_board);

            await ResolveCascadesAsync();

            if (_isLevelOver)
            {
                _isBusy = false;
                return;
            }

            if (TryHandleLevelFailed())
            {
                _isBusy = false;
                return;
            }

            await TryShowTriggeredBubbleAsync();

            _isBusy = false;
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
        
        await ResolveCascadesAsync();

        if (_isLevelOver)
        {
            _isBusy = false;
            return;
        }

        if (TryHandleLevelFailed())
        {
            _isBusy = false;
            return;
        }

        await TryShowTriggeredBubbleAsync();

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
        }
    }

    public async void TryRemoveCellsFromGrid(List<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0)
            return;

        var uniqueCells = new HashSet<Vector2Int>(cells);

        var fallMoves = new List<Board.FallMove>();
        var spawns = new List<Board.SpawnInfo>();

        _board.ClearCells(uniqueCells, fallMoves, spawns);
        UpdateGoalUI();

        await _view.AnimateGravity(fallMoves, spawns, _board, 0.1f);
        await ResolveCascadesAsync();
        await TryShowTriggeredBubbleAsync();
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
            AudioManager.instance.PlaySFX(13);
        }

        _view.SwapsEnabled = true;
    }

    private async void EndTimerBomb()
    {
        _timerBombResolving = true;
        IsTimerBombActive = false;

        OnTimerBombStateChanged?.Invoke(false);


        _view.SetTimerVisible(false);

        // freeze input while resolving
        _view.SwapsEnabled = false;
        _isBusy = true;

        if (AudioManager.instance != null)
            AudioManager.instance.PlayBG((int)_cfg.songNumber);

        await ResolveCascadesAsync();

        if (!_isLevelOver)
        {
            if (TryHandleLevelFailed())
            {
                _isBusy = false;
                _timerBombResolving = false;
                return;
            }

            await TryShowTriggeredBubbleAsync();
        }

        _isBusy = false;
        _view.SwapsEnabled = !_isLevelOver;
        _timerBombResolving = false;
    }

    private async Task ResolveCascadesAsync()
    {
        if (TryHandleLevelComplete())
            return;

        var matches = _board.FindMatches();
        while (matches.Count > 0)
        {
            await _view.AnimateMatchPopFx(matches, 0.12f);

            var fallMoves = new List<Board.FallMove>();
            var spawns = new List<Board.SpawnInfo>();

            _board.ResolveMatches(matches, fallMoves, spawns);

            UpdateGoalUI();
            await _view.AnimateGravity(fallMoves, spawns, _board, 0.20f);
            UpdateGoalUI();

            if (TryHandleLevelComplete())
                return;

            matches = _board.FindMatches();

            if (!_isLevelOver && !AreAllGoalsComplete())
                await EnsurePlayableBoardAsync();
        }

        // Only after cascades are fully done:
        _board.ResolveWolfSheepAfterCascades();

        while (_pendingWolfEats.Count > 0)
        {
            var eat = _pendingWolfEats.Dequeue();

            // 1. Wolf nudges
            await _view.AnimateWolfNudge(eat.wolf, eat.sheep, 0.20f);

            // 2. Only now this sheep turns into bones
            _view.RefreshCellSprite(eat.sheep, _board);

            UpdateGoalUI();
        }

        TryHandleLevelComplete();

        if (!_isLevelOver)
            await EnsurePlayableBoardAsync();
    }

    //private async Task<bool> TryHandleLevelCompleteAsync()
    private bool TryHandleLevelComplete()
    {
        if (_isLevelOver || !AreAllGoalsComplete())
            return false;

        _isLevelOver = true;
        
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
        {
            _levelClearedDimmer.SetActive(true);
        }

        if (_levelClearedPopupUI != null)
        {
            _levelClearedPopupUI.Show(level, finalScore, coins, stars);
        }
        else
        {
            Debug.LogError("LevelClearedPopupUI is not assigned on BoardController.");
        }

        if (AudioManager.instance != null)
        {
            AudioManager.instance.ChangeMusicVolume(0.4f);
            AudioManager.instance.PlaySFX(16);
            //await WaitFrames(25);
            AudioManager.instance.ChangeMusicVolume(1f);
        }

        return true;
    }

    private void UpdateGoalUI()
    {
        // last-level style - points and collectGoals
        if (_cfg.goalType == PointsOrMatches.points && HasCollectGoals)
        {
            _view.SetPointsAndCollectGoals(_board.CurrentPoints, _cfg.goal, _cfg.collectGoals, _collected);
            return;
        }

        else if (_cfg.goalType == PointsOrMatches.collectAnimals)
        {
            _view.SetCollectGoals(_cfg.collectGoals, _collected);
        }

        if (_board.GoalType == PointsOrMatches.points)
        {
            _view.SetScore(_board.CurrentPoints, _board.GoalAmount);
        }
        else if (_board.GoalType == PointsOrMatches.matches)
        {
            _view.SetMatchedAnimals(_board.MatchedAnimals, _board.GoalAmount);
        }

        _view.ShowGoal(true); // Show the goal text
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
    public async Task ShowHintOrHandleDeadBoardAsync()
    {
        if (_isBusy || _isLevelOver || _board == null)
            return;

        if (_hintFinder.TryFindHint(_board, out HintMove hint))
        {
            await _view.AnimateHint(hint.From, hint.To);
        }
        else
        {
            await EnsurePlayableBoardAsync();
        }
    }
    private async Task EnsurePlayableBoardAsync()
    {
        if (_isLevelOver) return;
        if (!IsTimerBombActive && _moveCounter.MovesLeft <= 0) return;

        if (_hintFinder.TryFindHint(_board, out _))
            return;

        _isBusy = true;
        _view.SwapsEnabled = false;

        await _view.ShowShuffleMessage(0.35f);

        int safety = 0;
        bool playable = false;

        do
        {
            _board.ShuffleSwappablePieces();
            safety++;

            playable = !_board.HasAnyMatch() && _hintFinder.TryFindHint(_board, out _);
        }
        while (!playable && safety < 100);

        await _view.AnimateShuffle(_board, 0.07f, 0.09f, 0.008f);

        _view.SwapsEnabled = true;
        _isBusy = false;
    }

    //private async Task WaitFrames(int frameCount)
    //{
    //    for (int i = 0; i < frameCount; i++)
    //        await Task.Yield();
    //}

    private void UpdateTimerUI()
    {
        float remaining = Mathf.Max(0f, _timerBombEndTime - Time.time);
        int seconds = Mathf.CeilToInt(remaining);

        if (seconds == _lastShownTimerSecond)
            return;

        _lastShownTimerSecond = seconds;
        _view.SetTimerSeconds(seconds);
    }
    private bool AreAllGoalsComplete()
    {
        // points or total matches
        bool primaryComplete = _cfg.goalType == PointsOrMatches.collectAnimals ? IsCollectGoalComplete() : _board.IsGoalReached;

        bool collectComplete = HasCollectGoals ? IsCollectGoalComplete() : true;

        return primaryComplete && collectComplete;
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
    private bool IsAnySheep(Animal piece)
    {
        return IsAnimal(piece, _cfg.blackSheep);
    }

    private async Task HandleLevelStartBubblesAsync()
    {
        if (_speechConfig == null || _speechBubblePresenter == null || _isLevelOver)
            return;

        if (_speechConfig.TryGetTutorialLevel(_cfg.levelIndex, out var tutorialLevel))
        {
            await ShowTutorialLevelAsync(tutorialLevel);
            return;
        }

        //await ShowRandomNormalBubbleAsync();
        await _animalDialogueController.ShowRandomNormalBubbleAsync(
            _board.GetAllCells(),
            cell => _board.GetAnimalFromCell(cell),
            cell => _view.GetCellWorldPosition(cell),
            (cell, duration) => _view.AnimateBlockedTap(cell, duration),
            _cfg.weidth
        );
    }

    private async Task ShowTutorialLevelAsync(TutorialLevelEntry tutorialLevel)
    {
        if (tutorialLevel.steps == null || tutorialLevel.steps.Count == 0)
            return;

        _bubbleActive = true;
        _isBusy = true;
        _view.SwapsEnabled = false;

        try
        {
            for (int i = 0; i < tutorialLevel.steps.Count; i++)
            {
                var step = tutorialLevel.steps[i];
                if (step.animal == null || step.lines == null || step.lines.Count == 0)
                    continue;

                bool useRightSide = step.side == SpeechSide.Right;
                var matchingCells = _board.FindCellsWithAnimal(step.animal);

                if (matchingCells.Count > 0)
                {
                    var speakerCell = matchingCells[UnityEngine.Random.Range(0, matchingCells.Count)];
                    _board.SetLockedCells(new[] { speakerCell });
                    _view.SetTutorialLockedCell(speakerCell);
                    await _view.AnimateBlockedTap(speakerCell, _speechCellHighlightDuration);
                    await PlayAnimalSpeakSfx(step.animal);    // Animal sound
                    await _speechBubblePresenter.ShowTutorialAsync(step.animal._sprite, step.lines, useRightSide);
                    _board.ClearLockedCells();
                    _view.SetTutorialLockedCell(null);
                }
                else
                {
                    await _speechBubblePresenter.ShowTutorialAsync(step.animal._sprite, step.lines, useRightSide);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"Tutorial bubble ended early: {ex.Message}");
        }
        finally
        {
            _bubbleActive = false;
            _isBusy = false;
            _view.SwapsEnabled = true;
            _board.ClearLockedCells();
            _view.SetTutorialLockedCell(null);
        }
    }

    //private async Task ShowRandomNormalBubbleAsync()
    //{
    //    float delay = Mathf.Max(0f, _normalBubbleDelaySeconds);
    //    if (delay > 0f)
    //        await Task.Delay(Mathf.RoundToInt(delay * 1000f));

    //    if (_isLevelOver || _speechConfig == null || _speechBubblePresenter == null)
    //        return;

    //    var allCells = new List<Vector2Int>();
    //    for (int x = 0; x < _board.Width; x++)
    //        for (int y = 0; y < _board.Height; y++)
    //            allCells.Add(new Vector2Int(x, y));

    //    for (int i = allCells.Count - 1; i > 0; i--)
    //    {
    //        int j = UnityEngine.Random.Range(0, i + 1);
    //        (allCells[i], allCells[j]) = (allCells[j], allCells[i]);
    //    }

    //    for (int i = 0; i < allCells.Count; i++)
    //    {
    //        var coord = allCells[i];
    //        var animal = _board.GetAnimalFromCell(coord);
    //        if (_speechConfig.TryGetRandomNormalLines(animal, out var options))
    //        {
    //            int randomIndex = UnityEngine.Random.Range(0, options.Count);
    //            var lines = new List<string> { options[randomIndex] };
    //            bool useRightSide = coord.x >= (_board.Width * 0.5f);
    //            await _view.AnimateBlockedTap(coord, _speechCellHighlightDuration);
    //            await PlayAnimalSpeakSfx(animal); // Animal Sound
    //            await _speechBubblePresenter.ShowNormalAsync(lines, _view.GetCellWorldPosition(coord), useRightSide, _normalBubbleVisibleSeconds);
    //            return;
    //        }
    //    }
    //}

    private async Task TryShowTriggeredBubbleAsync()
    {
        if (_bubbleActive || _isLevelOver || _speechConfig == null || _speechBubblePresenter == null)
            return;

        var entries = _speechConfig.GetTriggeredEntriesForLevel(_cfg.levelIndex);
        if (entries.Count == 0)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (_triggeredEntryIndicesShown.Contains(i))
                continue;

            var e = entries[i];
            if (e.triggerAnimal == null || e.lines == null || e.lines.Count == 0)
                continue;

            var triggerCells = _board.FindCellsWithAnimal(e.triggerAnimal);
            if (triggerCells.Count == 0)
                continue;

            var triggerCell = triggerCells[UnityEngine.Random.Range(0, triggerCells.Count)];

            Vector2Int speakerCell;
            Animal speakerAnimal;
            if (e.speakerAnimal != null)
            {
                var speakerCells = _board.FindCellsWithAnimal(e.speakerAnimal);
                if (speakerCells.Count == 0)
                    continue;

                speakerCell = speakerCells[UnityEngine.Random.Range(0, speakerCells.Count)];
                speakerAnimal = e.speakerAnimal;
            }
            else
            {
                var candidates = new List<Vector2Int>();
                for (int x = 0; x < _board.Width; x++)
                {
                    for (int y = 0; y < _board.Height; y++)
                    {
                        var c = new Vector2Int(x, y);
                        if (_board.GetAnimalFromCell(c) != null)
                            candidates.Add(c);
                    }
                }

                if (candidates.Count == 0)
                    continue;

                speakerCell = candidates[UnityEngine.Random.Range(0, candidates.Count)];
                speakerAnimal = _board.GetAnimalFromCell(speakerCell);
                if (speakerAnimal == null)
                    continue;
            }

            int randomLineIndex = UnityEngine.Random.Range(0, e.lines.Count);
            var lines = new List<string> { e.lines[randomLineIndex] };

            if (triggerCell == speakerCell)
                await _view.AnimateBlockedTap(triggerCell, _speechCellHighlightDuration);
            else
                await Task.WhenAll(
                    _view.AnimateBlockedTap(triggerCell, _speechCellHighlightDuration),
                    _view.AnimateBlockedTap(speakerCell, _speechCellHighlightDuration));

            bool useRightSide = UnityEngine.Random.Range(0, 2) == 1;

            _bubbleActive = true;
            try
            {
                await PlayAnimalSpeakSfx(speakerAnimal); // Animal Sound
                await _speechBubblePresenter.ShowTriggeredAsync(speakerAnimal._sprite, lines, useRightSide, _triggeredBubbleVisibleSeconds);
                _triggeredEntryIndicesShown.Add(i);
            }
            finally
            {
                _bubbleActive = false;
            }
            return;
        }
    }

    private async Task PlayAnimalSpeakSfx(Animal animal)
    {
        if (animal == null || animal._speakSfxId == 0) return;
        if (AudioManager.instance == null) return;

        AudioManager.instance.PlaySFX(animal._speakSfxId);

        if (_speakSfxDuration > 0f)
            await Task.Delay(Mathf.CeilToInt(_speakSfxDuration * 1000f));
    }

    private async Task AnimateBlackSheepBlastFromCenter(Vector2Int center, bool swipedVertically)
    {
        // vertical swipe => ROW blast (left + right)
        // horizontal swipe => COLUMN blast (up + down)

        int maxWave = swipedVertically
            ? Mathf.Max(center.x, (_cfg.weidth - 1) - center.x)
            : Mathf.Max(center.y, (_cfg.height - 1) - center.y);

        if (AudioManager.instance != null && !swipedVertically) // PLay longer sound
        {
            AudioManager.instance.PlaySFXPitchAdjusted(17);
        }

        if (AudioManager.instance != null && swipedVertically) // Play shorter sound
        {
            AudioManager.instance.PlaySFXPitchAdjusted(18);
        }

        for (int wave = 0; wave <= maxWave; wave++)
        {
            var waveCells = new List<Vector2Int>();

            if (swipedVertically)
            {
                int y = center.y;

                int leftX = center.x - wave;
                int rightX = center.x + wave;

                if (leftX >= 0)
                {
                    var leftCell = new Vector2Int(leftX, y);
                    if (_board.GetAnimalFromCell(leftCell) != _cfg.boneBlock)
                        waveCells.Add(leftCell);
                }

                if (rightX < _cfg.weidth && rightX != leftX)
                {
                    var rightCell = new Vector2Int(rightX, y);
                    if (_board.GetAnimalFromCell(rightCell) != _cfg.boneBlock)
                        waveCells.Add(rightCell);
                }
            }
            else
            {
                int x = center.x;

                int downY = center.y - wave;
                int upY = center.y + wave;

                if (downY >= 0)
                {
                    var downCell = new Vector2Int(x, downY);
                    if (_board.GetAnimalFromCell(downCell) != _cfg.boneBlock)
                        waveCells.Add(downCell);
                }

                if (upY < _cfg.height && upY != downY)
                {
                    var upCell = new Vector2Int(x, upY);
                    if (_board.GetAnimalFromCell(upCell) != _cfg.boneBlock)
                        waveCells.Add(upCell);
                }
            }

            if (waveCells.Count == 0)
                continue;

            if (AudioManager.instance != null)
                AudioManager.instance.PlaySFXPitchAdjusted(8, 0.2f);

            await _view.AnimateMatchPopFx(waveCells, 0.09f);
        }
    }

    private bool TryHandleLevelFailed()
    {
        if (_isLevelOver) return true;
        if (AreAllGoalsComplete()) return false;
        if (_moveCounter.MovesLeft > 0) return false;

        _isLevelOver = true;
        _view.SwapsEnabled = false;

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
}
