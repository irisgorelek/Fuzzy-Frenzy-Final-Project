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
/// decides which rules and step players a level uses, so adding an animal ability means adding its rule
/// and its step player here, never editing the turn code or the presenter.
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
            new SpecialSpawnSettings(board.Catalog.TypeOf(config.blackSheep), config.blackSheepUnlockAfterMoves, config.blackSheepRollChance));

        var presenter = new TurnPresenter(view, board.Catalog, listener, StepPlayersFor(config));
        return new LevelRuntime(board, game, presenter);
    }

    /// <summary>
    /// The rules a level plays with, in priority order. Every level uses the same set today: the rules act on
    /// tile traits and diets from the Animal assets, so a level only gets behaviour for the animals it has.
    /// </summary>
    public static IReadOnlyList<IActionRule> RulesFor(BoardConfig config) => Match3Game.DefaultRules();

    /// <summary>How each kind of turn step is shown. Add the player for a new special's step here.</summary>
    public static IEnumerable<ITurnStepPlayer> StepPlayersFor(BoardConfig config) => TurnPresenter.DefaultPlayers();
}
