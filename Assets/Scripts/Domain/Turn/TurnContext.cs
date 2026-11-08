namespace FuzzyFrenzy.Domain
{
    /// <summary>The state the core needs from outside to play a turn.</summary>
    public readonly struct TurnContext
    {
        public TurnContext(int movesLeft, int maxMoves, bool timerActive)
        {
            MovesLeft = movesLeft;
            MaxMoves = maxMoves;
            TimerActive = timerActive;
        }

        public int MovesLeft { get; }
        public int MaxMoves { get; }
        public bool TimerActive { get; }
    }
}
