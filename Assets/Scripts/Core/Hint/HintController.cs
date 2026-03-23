using UnityEngine;

public class HintController : MonoBehaviour
{
    [SerializeField] private BoardController _boardController;
    [SerializeField] private BoardView _boardView;
    [SerializeField] private GameObject _noMovesPopup;

    private BoardHintFinder _hintFinder = new BoardHintFinder();

    public void OnHintButtonPressed()
    {
        if (_boardController == null || _boardView == null)
            return;

        Board board = _boardController.CurrentBoard;
        if (board == null)
            return;

        if (_hintFinder.TryFindHint(board, out HintMove hint)) // a way to make a match was found
        {
            _ = _boardView.AnimateHint(hint.From, hint.To);
        }
        else
        {
            if (_noMovesPopup != null) // no options for matches found -> activate PopUp
                _noMovesPopup.SetActive(true);
        }
    }
}