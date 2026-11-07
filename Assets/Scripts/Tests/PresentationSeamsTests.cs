using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FuzzyFrenzy.Domain;
using NUnit.Framework;
using UnityEngine;

/// <summary>The Unity-side seams: step players, the level state machine and goal progress.</summary>
public class PresentationSeamsTests
{
    [Test]
    public void Presenter_UsesTheRegisteredPlayerForEachStep()
    {
        var player = new RecordingPlayer();
        var presenter = new TurnPresenter(new FakeAnimator(), new AnimalCatalog(), new NullListener(), new ITurnStepPlayer[] { player });

        var steps = new TurnStep[] { new CustomStep(), new CustomStep() };
        var played = presenter.PlayAsync(new TurnResult(TurnOutcome.Resolved, RejectReason.None, steps, false), CancellationToken.None);

        Assert.That(played.IsCompleted && played.Result, Is.True);
        Assert.That(player.Played, Is.EqualTo(2));
    }

    [Test]
    public void Presenter_SkipsStepsWithoutAPlayer()
    {
        var presenter = new TurnPresenter(new FakeAnimator(), new AnimalCatalog(), new NullListener(), new ITurnStepPlayer[0]);

        var played = presenter.PlayAsync(new TurnResult(TurnOutcome.Resolved, RejectReason.None, new TurnStep[] { new CustomStep() }, false), CancellationToken.None);

        Assert.That(played.IsCompleted && played.Result, Is.True);
    }

    [Test]
    public void LevelFlow_EndsOnceAndLocksInput()
    {
        var input = new InputGate();
        var flow = new LevelFlow(input);

        Assert.That(flow.TryEnd(LevelPhase.Won), Is.True);
        Assert.That(flow.TryEnd(LevelPhase.Lost), Is.False, "A won level can't also be lost.");
        Assert.That(flow.Phase, Is.EqualTo(LevelPhase.Won));
        Assert.That(input.IsOpen, Is.False);

        flow.Dispose();
        Assert.That(input.IsOpen, Is.True, "Restarting the level releases the lock.");
    }

    [Test]
    public void GoalTracker_ShowsProgressAsTheTurnReplays()
    {
        var config = ScriptableObject.CreateInstance<BoardConfig>();
        try
        {
            config.goalType = PointsOrMatches.points;
            config.goal = 50;
            var hud = new FakeHud();
            var tracker = new GoalProgressTracker(config, hud);

            tracker.AddScored(12, 3);
            tracker.Render();

            Assert.That(hud.Points, Is.EqualTo(12));
            Assert.That(hud.Goal, Is.EqualTo(50));
        }
        finally { Object.DestroyImmediate(config); }
    }

    private sealed class CustomStep : TurnStep { }

    private sealed class RecordingPlayer : TurnStepPlayer<CustomStep>
    {
        public int Played;
        protected override Task Play(CustomStep step, PresentationContext context)
        {
            Played++;
            return Task.CompletedTask;
        }
    }

    private sealed class NullListener : ITurnPresentationListener
    {
        public void OnMoveUsed() { }
        public void OnTilesScored(int points, int matched) { }
        public void OnTilesDestroyed(string tileId, int count) { }
        public void OnBoneBroken(Vector2Int cell) { }
        public void OnGoalProgressChanged() { }
    }

    private sealed class FakeHud : IBoardHud
    {
        public int Points, Goal;
        public void SetLevelNumber(int level) { }
        public void SetBackground(Sprite backgroundSprite) { }
        public void ShowGoal(bool show) { }
        public void SetScore(int points, int totalPoints) { Points = points; Goal = totalPoints; }
        public void SetMatchedAnimals(int animals, int goal) { }
        public void SetCollectGoals(List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected) { }
        public void SetPointsAndCollectGoals(int points, int pointsGoal, List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected) { }
        public void SetMovesText(int movesLeft) { }
        public void SetMovesLastMoveTension(bool active) { }
        public void SetTimerVisible(bool visible) { }
        public void SetTimerSeconds(int seconds) { }
    }

    private sealed class FakeAnimator : IBoardAnimator
    {
        public int Width => 3;
        public int Height => 3;
        public void SetCellAnimal(Vector2Int coord, Animal animal) { }
        public Task AnimateSwap(Vector2Int a, Vector2Int b, float duration) => Task.CompletedTask;
        public Task AnimateGravity(List<Board.FallMove> moves, List<Board.SpawnInfo> spawns, IAnimalGrid board, float duration) => Task.CompletedTask;
        public Task AnimateWolfNudge(Vector2Int predatorCell, Vector2Int preyCell, float duration) => Task.CompletedTask;
        public Task ShowShuffleMessage(float hold) => Task.CompletedTask;
        public Task AnimateShuffle(IAnimalGrid board, float outDuration, float inDuration, float stagger) => Task.CompletedTask;
        public Task AnimateMatchPopFx(List<Vector2Int> matches, float duration, BoardClearEffect effect) => Task.CompletedTask;
        public Task AnimateSheepAnger(Vector2Int coord) => Task.CompletedTask;
        public void PlayBlackSheepFx(Vector2Int coord, bool horizontal, int width, int height) { }
        public Task WaitForBlackSheepParticles() => Task.CompletedTask;
    }
}
