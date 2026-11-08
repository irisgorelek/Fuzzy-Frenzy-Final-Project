using System.Collections.Generic;
using FuzzyFrenzy.Domain;

/// <summary>
/// Builds a level's runtime from its config. This is the one place that wires the core to Unity and
/// decides which rules and step players a level uses, so adding an animal ability means adding its rule
/// and its step player here, never editing the turn code or the presenter.
/// </summary>
public static class LevelFactory
{
    /// <summary>The level's board, session and tile catalog. Built first so the level's listeners can see the session.</summary>
    public static Board CreateBoard(BoardConfig config, IRandomSource random = null) =>
        new Board(config, random ?? new UnityRandomSource());

    /// <summary>The turn engine and its presenter for a board made by <see cref="CreateBoard"/>.</summary>
    public static LevelRuntime Create(Board board, BoardConfig config, IBoardAnimator view, ITurnPresentationListener listener, IGameAudio audio)
    {
        var game = new Match3Game(
            board.State,
            board.Session,
            board.Random,
            RulesFor(config),
            new SpecialSpawnSettings(board.Catalog.TypeOf(config.blackSheep), config.blackSheepUnlockAfterMoves, config.blackSheepRollChance));

        var presenter = new TurnPresenter(view, board.Catalog, listener, audio, StepPlayersFor(config));
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
