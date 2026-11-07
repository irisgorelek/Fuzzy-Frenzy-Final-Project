using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;
using UnityEngine;

/// <summary>Receives the gameplay side effects of a turn at the moment the player sees them.</summary>
public interface ITurnPresentationListener
{
    void OnMoveUsed();
    /// <summary>Tiles were scored. <paramref name="matched"/> counts toward "matches" goals.</summary>
    void OnTilesScored(int points, int matched);
    void OnTilesDestroyed(string tileId, int count);
    void OnBoneBroken(Vector2Int cell);
    void OnGoalProgressChanged();
}

/// <summary>
/// Replays a <see cref="TurnResult"/> on the board view, step by step. The core has already resolved the
/// whole turn; this only decides what the player sees and hears, and when. Each step type is shown by its
/// own <see cref="ITurnStepPlayer"/>, so a new kind of step never changes this class.
/// </summary>
public sealed class TurnPresenter
{
    private readonly PresentationContext _context;
    private readonly Dictionary<Type, ITurnStepPlayer> _players = new Dictionary<Type, ITurnStepPlayer>();

    public TurnPresenter(IBoardAnimator view, AnimalCatalog catalog, ITurnPresentationListener listener,
        IEnumerable<ITurnStepPlayer> players = null)
    {
        _context = new PresentationContext(view, catalog, listener);
        foreach (var player in players ?? DefaultPlayers())
            Register(player);
    }

    /// <summary>A fresh set of the players every level uses.</summary>
    public static ITurnStepPlayer[] DefaultPlayers() => new ITurnStepPlayer[]
    {
        new SwapStepPlayer(),
        new SwapBackStepPlayer(),
        new MoveUsedStepPlayer(),
        new MatchPopStepPlayer(),
        new ClearStepPlayer(),
        new BonesBrokenStepPlayer(),
        new BlackSheepBlastStepPlayer(),
        new GravityStepPlayer(),
        new FeastStepPlayer(),
        new ShuffleStepPlayer(),
    };

    /// <summary>Adds a player, replacing any player already registered for the same step type.</summary>
    public void Register(ITurnStepPlayer player)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        _players[player.StepType] = player;
    }

    /// <summary>Plays every step in order. Returns false if it stopped early because the scene is closing.</summary>
    public async Task<bool> PlayAsync(TurnResult result, CancellationToken cancellation)
    {
        if (result == null) return true;

        foreach (var step in result.Steps)
        {
            if (cancellation.IsCancellationRequested || IsViewGone)
                return false;

            await PlayerFor(step).PlayAsync(step, _context);
        }

        return !cancellation.IsCancellationRequested;
    }

    // The view is a Unity object behind an interface, so check for a destroyed one explicitly.
    private bool IsViewGone => _context.View == null || (_context.View is UnityEngine.Object unityView && unityView == null);

    private ITurnStepPlayer PlayerFor(TurnStep step)
    {
        // Exact type first, then base types, so a player can cover a family of steps.
        for (var type = step.GetType(); type != null && type != typeof(TurnStep); type = type.BaseType)
            if (_players.TryGetValue(type, out var player))
                return player;

        Debug.LogWarning($"TurnPresenter: no presentation for {step.GetType().Name}.");
        return MissingPlayer.Instance;
    }

    private sealed class MissingPlayer : ITurnStepPlayer
    {
        public static readonly MissingPlayer Instance = new MissingPlayer();
        public Type StepType => typeof(TurnStep);
        public Task PlayAsync(TurnStep step, PresentationContext context) => Task.CompletedTask;
    }
}
