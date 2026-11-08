using System.Threading;
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
        var presenter = new TurnPresenter(new FakeAnimator(), new AnimalCatalog(), new NullListener(), SilentAudio.Instance, new ITurnStepPlayer[] { player });

        var steps = new TurnStep[] { new CustomStep(), new CustomStep() };
        var played = presenter.PlayAsync(new TurnResult(TurnOutcome.Resolved, RejectReason.None, steps, false), CancellationToken.None);

        Assert.That(played.IsCompleted && played.Result, Is.True);
        Assert.That(player.Played, Is.EqualTo(2));
    }

    [Test]
    public void Presenter_SkipsStepsWithoutAPlayer()
    {
        var presenter = new TurnPresenter(new FakeAnimator(), new AnimalCatalog(), new NullListener(), SilentAudio.Instance, new ITurnStepPlayer[0]);

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
}
