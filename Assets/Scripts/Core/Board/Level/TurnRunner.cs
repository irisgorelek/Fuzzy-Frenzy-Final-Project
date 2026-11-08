using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;
using UnityEngine;

/// <summary>
/// Plays every kind of turn: a swap, a free swap during the timer booster, the bomb, the board settling when
/// the timer ends, and the shuffle when no move is left. Each one asks <see cref="Match3Game"/> for the
/// result, replays it through <see cref="TurnPresenter"/> while holding the input lock, and then checks for
/// a win or loss and any dialogue. Whether input is allowed at all is the controller's call.
/// </summary>
public sealed class TurnRunner
{
    private readonly Board _board;
    private readonly Match3Game _game;
    private readonly TurnPresenter _presenter;
    private readonly InputGate _input;
    private readonly LevelProgress _progress;
    private readonly LevelDialogue _dialogue;
    private readonly TimerBoosterClock _timer;
    private readonly MoveCounter _moves;
    private readonly BoardView _view;
    private readonly IGameAudio _audio;
    private readonly BoardConfig _config;
    private readonly CancellationToken _cancellation;
    private readonly BoardHintFinder _hintFinder = new BoardHintFinder();

    private Task _freeSwap = Task.CompletedTask;

    public TurnRunner(LevelRuntime level, LevelProgress progress, LevelDialogue dialogue, TimerBoosterClock timer,
        MoveCounter moves, InputGate input, BoardView view, IGameAudio audio, BoardConfig config, CancellationToken cancellation)
    {
        _board = level.Board;
        _game = level.Game;
        _presenter = level.Presenter;
        _progress = progress;
        _dialogue = dialogue;
        _timer = timer;
        _moves = moves;
        _input = input;
        _view = view;
        _audio = audio;
        _config = config;
        _cancellation = cancellation;
    }

    /// <summary>True from the moment the timer booster runs out until the board has settled.</summary>
    public bool IsResolvingTimer { get; private set; }

    // ---- Swaps ----

    public async Task SwapAsync(Vector2Int from, Vector2Int to)
    {
        if (_timer.IsActive)
        {
            await FreeSwapAsync(from, to);
            return;
        }

        if (_moves.MovesLeft <= 0)
            return;

        using (_input.Acquire("turn"))
        {
            var result = _game.TrySwap(from.ToGrid(), to.ToGrid(), CurrentContext());

            if (result.Outcome == TurnOutcome.Rejected)
            {
                if (result.RejectReason != RejectReason.EmptyCell)
                    await PlayInvalidSwapAsync(from, to);
                return;
            }

            if (!await _presenter.PlayAsync(result, _cancellation))
                return;

            if (result.Outcome == TurnOutcome.Resolved)
                await FinishTurnAsync(result, checkFailure: true);
        }
    }

    // During the timer booster swaps cost nothing and nothing matches until time runs out.
    private async Task FreeSwapAsync(Vector2Int from, Vector2Int to)
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
            _freeSwap = _presenter.PlayAsync(result, _cancellation);
            await _freeSwap;
        }
    }

    private Task PlayInvalidSwapAsync(Vector2Int from, Vector2Int to)
    {
        _audio.PlaySfx(SfxId.CannotSwap);
        return _view.AnimateInvalidSwap(from, to, to - from);
    }

    // ---- Boosters ----

    /// <summary>Clears the bomb's area and plays out the cascade. The bomb plays its own impact animation first.</summary>
    public async Task ClearCellsAsync(IReadOnlyList<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0)
            return;

        using (_input.Acquire("turn"))
        {
            var result = _game.ClearCells(cells.ToGrid(), CurrentContext());

            if (!await _presenter.PlayAsync(result, _cancellation))
                return;

            await FinishTurnAsync(result, checkFailure: false);
        }
    }

    public void StartTimer(float durationSeconds)
    {
        if (_progress.IsOver) return;

        IsResolvingTimer = false;
        _timer.Start(durationSeconds);
    }

    /// <summary>Ends the timer booster and resolves every match the free swaps made.</summary>
    public async Task EndTimerAsync()
    {
        IsResolvingTimer = true;
        _timer.Stop();

        try
        {
            using (_input.Acquire("turn"))
            {
                await _freeSwap; // let the last free swap finish moving first

                _audio.PlayLevelMusic(_config.songNumber);

                var result = _game.ResolveBoard(CurrentContext());
                if (await _presenter.PlayAsync(result, _cancellation))
                    await FinishTurnAsync(result, checkFailure: true);
            }
        }
        finally
        {
            IsResolvingTimer = false;
        }
    }

    // ---- Hint and dead board ----

    /// <summary>Shows a possible move, or shuffles the board if there is none.</summary>
    public async Task ShowHintOrShuffleAsync()
    {
        if (_hintFinder.TryFindHint(_board, out HintMove hint))
            await _view.AnimateHint(hint.From, hint.To);
        else
            await EnsurePlayableAsync();
    }

    /// <summary>Shuffles the board if no move is possible.</summary>
    public async Task EnsurePlayableAsync()
    {
        if (_progress.IsOver) return;

        var result = _game.EnsurePlayable(CurrentContext());
        if (result.Steps.Count == 0) return;

        using (_input.Acquire("shuffle"))
            await _presenter.PlayAsync(result, _cancellation);
    }

    // ---- After a turn ----

    /// <summary>What happens after a turn has played out: win, lose, then any triggered dialogue.</summary>
    private async Task FinishTurnAsync(TurnResult result, bool checkFailure)
    {
        if (result.GoalsComplete)
            _progress.TryWin();

        _dialogue.HideNormalBubble();

        if (_progress.IsOver)
            return;

        if (checkFailure && _progress.TryLose())
            return;

        await _dialogue.PlayTriggeredAsync(_board, _view);
    }

    private TurnContext CurrentContext() => new TurnContext(_moves.MovesLeft, _config.maxMoves, _timer.IsActive);
}
