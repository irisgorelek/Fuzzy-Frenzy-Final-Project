using System;
using UnityEngine;

public class MoveCounter : MonoBehaviour
{
    private int _movesLeft;
    public int MovesLeft => _movesLeft;

    public Action OnMovesOver;
    public event Action<int> OnMovesChanged;

    public void InitializeMoves(int maxMoves)
    {
        _movesLeft = maxMoves;
        OnMovesChanged?.Invoke(_movesLeft);
    }

    public void UseMove()
    {
        if (_movesLeft <= 0) // final valid move for player
            return;

        _movesLeft--;
        OnMovesChanged?.Invoke(_movesLeft);

        if (_movesLeft == 0)
            OnMovesOver?.Invoke(); // player finished his last move
    }
}
