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
