public sealed class BoardControllerGameInitializer
{
    private readonly BoardController _controller;

    public BoardControllerGameInitializer(BoardController controller)
    {
        _controller = controller;
    }

    public void InitializeGame()
    {
        // Technical
        _controller.Board = new Board(_controller.Cfg);
        _controller.TriggeredEntryIndicesShown.Clear();

        _controller.Board.OnWolfAteSheep += (wolf, sheep) =>
        {
            _controller.PendingWolfEats.Enqueue((wolf, sheep));
        };

        _controller.GoalTracker = new BoardControllerGoalTracker(_controller.Cfg, _controller.View, _controller.AnimalsDestroyedChannelSO);
        _controller.GoalTracker.Reset();
        _controller.Board.OnAnimalsDestroyed = (animalId, count) => _controller.GoalTracker.HandleAnimalsDestroyed(animalId, count, _controller.Board);

        //_board.OnScoreAdded = amount => _scoreEventChannelSO.RaiseEvent(amount);
        _controller.Board.OnScoreAdded += amount => _controller.LevelScoreEventChannelSO.RaiseEvent(amount); // In-Level

        _controller.MoveCounter.InitializeMoves(_controller.Cfg.maxMoves);

        _controller.Board.Initialize();

        //_board.InitializeStaticDeadBoard(); // For the shuffle tests

        //_blackSheepTriggered = false;

        // Visual
        _controller.View.Build(_controller.Cfg.weidth, _controller.Cfg.height);
        _controller.View.AssignSprites(_controller.Board);
        _controller.View.SetLevelNumber(_controller.Cfg.levelIndex);
        _controller.View.SetBackground(_controller.Cfg.BackgroundSprite);
    }
}
