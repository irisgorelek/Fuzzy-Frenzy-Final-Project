using System.Collections.Generic;
using FuzzyFrenzy.Domain;

/// <summary>
/// Goal progress as the player has seen it so far, and drawing it on the HUD. The core is ahead of this
/// while a turn replays; the presenter feeds it step by step so the counters move with the animation.
/// </summary>
public sealed class GoalProgressTracker
{
    private readonly Dictionary<string, int> _collected = new Dictionary<string, int>();
    private readonly BoardConfig _config;
    private readonly IBoardHud _hud;

    public GoalProgressTracker(BoardConfig config, IBoardHud hud)
    {
        _config = config;
        _hud = hud;
    }

    public int ShownPoints { get; private set; }
    public int ShownMatched { get; private set; }
    public IReadOnlyDictionary<string, int> Collected => _collected;

    public bool HasCollectGoals => _config.collectGoals != null && _config.collectGoals.Count > 0;

    public void AddScored(int points, int matched)
    {
        ShownPoints += points;
        ShownMatched += matched;
    }

    public void AddDestroyed(string tileId, int count)
    {
        _collected.TryGetValue(tileId, out int have);
        _collected[tileId] = have + count;
    }

    /// <summary>Makes the shown progress match the core, for example after a debug cheat.</summary>
    public void SyncWith(GameSession session)
    {
        ShownPoints = session.Points;
        ShownMatched = session.MatchedAnimals;
        _collected.Clear();
        foreach (var entry in session.Collected)
            _collected[entry.Key] = entry.Value;
    }

    public void Render()
    {
        if (_hud == null)
            return;

        // Points plus collect goals (last-level style)
        if (_config.goalType == PointsOrMatches.points && HasCollectGoals)
        {
            _hud.SetPointsAndCollectGoals(ShownPoints, _config.goal, _config.collectGoals, _collected);
            return;
        }

        if (_config.goalType == PointsOrMatches.collectAnimals)
            _hud.SetCollectGoals(_config.collectGoals, _collected);

        if (_config.goalType == PointsOrMatches.points)
            _hud.SetScore(ShownPoints, _config.goal);
        else if (_config.goalType == PointsOrMatches.matches)
            _hud.SetMatchedAnimals(ShownMatched, _config.goal);

        _hud.ShowGoal(true);
    }
}
