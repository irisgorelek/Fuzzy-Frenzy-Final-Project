using System.Threading.Tasks;

public class BoardControllerPlayableBoardEnsurer
{
    private readonly BoardHintFinder _hintFinder = new BoardHintFinder();

    public bool TryFindHint(Board board, out HintMove hint)
    {
        return _hintFinder.TryFindHint(board, out hint);
    }

    public async Task ShowHintAsync(Board board, BoardView view)
    {
        if (_hintFinder.TryFindHint(board, out HintMove hint))
            await view.AnimateHint(hint.From, hint.To);
    }

    public async Task ShuffleUntilPlayableAsync(Board board, BoardView view)
    {
        view.SwapsEnabled = false;

        await view.ShowShuffleMessage(0.35f);

        int safety = 0;
        bool playable = false;

        do
        {
            board.ShuffleSwappablePieces();
            safety++;

            playable = !board.HasAnyMatch() && _hintFinder.TryFindHint(board, out _);
        }
        while (!playable && safety < 100);

        await view.AnimateShuffle(board, 0.07f, 0.09f, 0.008f);

        view.SwapsEnabled = true;
    }
}
