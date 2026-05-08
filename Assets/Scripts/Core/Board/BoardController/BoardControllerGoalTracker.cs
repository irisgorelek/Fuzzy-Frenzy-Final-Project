using System.Collections.Generic;
using UnityEngine;

public class BoardControllerGoalTracker
{
    private readonly BoardConfig _cfg;
    private readonly BoardView _view;
    private readonly AnimalsDestroyedEventChannelSO _animalsDestroyedChannelSO;

    private readonly Dictionary<string, int> _collected = new Dictionary<string, int>(); // Track collected animals

    public bool HasCollectGoals => _cfg.collectGoals != null && _cfg.collectGoals.Count > 0;

    public BoardControllerGoalTracker(BoardConfig cfg, BoardView view, AnimalsDestroyedEventChannelSO animalsDestroyedChannelSO)
    {
        _cfg = cfg;
        _view = view;
        _animalsDestroyedChannelSO = animalsDestroyedChannelSO;
    }

    public void Reset()
    {
        _collected.Clear();
    }

    public void DebugCompleteCollectGoals()
    {
        if (!HasCollectGoals || _cfg.collectGoals == null)
            return;

        foreach (var g in _cfg.collectGoals)
        {
            if (g.animal == null || string.IsNullOrEmpty(g.animal._id))
                continue;

            _collected[g.animal._id] = g.amount;
        }
    }

    // For the animal collection goal
    public void HandleAnimalsDestroyed(string animalId, int count, Board board)
    {
        _animalsDestroyedChannelSO.RaiseEvent(animalId, count);

        // Only track collection if this level is a collect level
        if (!HasCollectGoals) // && _cfg.goalType != PointsOrMatches.collectAnimals)
            return;

        if (!_collected.TryGetValue(animalId, out int have))
            have = 0;

        _collected[animalId] = have + count;

        // Update goal UI
        UpdateGoalUI(board);
    }

    public bool AreAllGoalsComplete(Board board)
    {
        // points or total matches
        bool primaryComplete = _cfg.goalType == PointsOrMatches.collectAnimals ? IsCollectGoalComplete() : board.IsGoalReached;

        bool collectComplete = HasCollectGoals ? IsCollectGoalComplete() : true;

        return primaryComplete && collectComplete;
    }

    public void UpdateGoalUI(Board board)
    {
        // last-level style - points and collectGoals
        if (_cfg.goalType == PointsOrMatches.points && HasCollectGoals)
        {
            _view.SetPointsAndCollectGoals(board.CurrentPoints, _cfg.goal, _cfg.collectGoals, _collected);
            return;
        }

        else if (_cfg.goalType == PointsOrMatches.collectAnimals)
        {
            _view.SetCollectGoals(_cfg.collectGoals, _collected);
        }

        if (board.GoalType == PointsOrMatches.points)
        {
            _view.SetScore(board.CurrentPoints, board.GoalAmount);
        }
        else if (board.GoalType == PointsOrMatches.matches)
        {
            _view.SetMatchedAnimals(board.MatchedAnimals, board.GoalAmount);
        }

        _view.ShowGoal(true); // Show the goal text
    }

    private bool IsCollectGoalComplete()
    {
        foreach (var g in _cfg.collectGoals)
        {
            if (g.animal == null) continue;

            _collected.TryGetValue(g.animal._id, out int have);
            if (have < g.amount) return false;
        }
        return true;
    }
}
