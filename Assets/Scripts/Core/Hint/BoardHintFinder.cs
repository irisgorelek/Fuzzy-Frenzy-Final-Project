using FuzzyFrenzy.Domain;

/// <summary>Finds a swap that would make a match, for the hint button and the dead-board check.</summary>
public class BoardHintFinder
{
    /// <summary>Picks one random legal move, so repeated hints don't always point at the same spot.</summary>
    public bool TryFindHint(Board board, out HintMove hint)
    {
        if (board != null && MoveFinder.TryFindMove(board.State, board.Random, out var move))
        {
            hint = new HintMove(move.From.ToVector(), move.To.ToVector());
            return true;
        }

        hint = default;
        return false;
    }
}
