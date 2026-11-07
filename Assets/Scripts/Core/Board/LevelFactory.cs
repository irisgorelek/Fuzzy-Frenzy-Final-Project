using System.Collections.Generic;
using FuzzyFrenzy.Domain;

/// <summary>Everything one play of a level runs on.</summary>
public sealed class LevelRuntime
{
    public LevelRuntime(Board board, Match3Game game, TurnPresenter presenter)
    {
        Board = board;
        Game = game;
        Presenter = presenter;
    }

    /// <summary>Unity-facing board: Animal assets in, core types underneath.</summary>
    public Board Board { get; }
    /// <summary>The turn owner.</summary>
    public Match3Game Game { get; }
    /// <summary>Replays turn results on the view.</summary>
    public TurnPresenter Presenter { get; }
}

/// <summary>
/// Builds a level's runtime from its config. This is the one place that wires the core to Unity and
/// decides which rules a level uses, so adding an animal ability means adding its rule here (or to the
/// level's config), never editing the turn code.
/// </summary>
public static class LevelFactory
{
    public static LevelRuntime Create(BoardConfig config, BoardView view, ITurnPresentationListener listener, IRandomSource random = null)
    {
        var board = new Board(config, random ?? new UnityRandomSource());

        var game = new Match3Game(
            board.State,
            board.Session,
            board.Random,
            RulesFor(config),
            new BlackSheepRollSettings(config.blackSheep != null, config.blackSheepUnlockAfterMoves, config.blackSheepRollChance));

        var presenter = new TurnPresenter(view, board.Catalog, listener);
        return new LevelRuntime(board, game, presenter);
    }

    /// <summary>The rules a level plays with, in priority order. Every level uses the same set today.</summary>
    public static IReadOnlyList<IActionRule> RulesFor(BoardConfig config) => Match3Game.DefaultRules();
}
