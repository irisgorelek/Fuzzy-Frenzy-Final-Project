using System.Collections.Generic;
using UnityEngine;

public class BoardViewGoalRowsPresenter
{
    private struct GoalRowSpec
    {
        public bool IsPrimary;
        public Sprite Icon;
        public string Text;
        public Color Color;
        public bool IsComplete;
    }

    private readonly Transform _goalRowsParent;
    private readonly GoalRowView _animalGoalRowPrefab;
    private readonly GoalRowView _primaryGoalRowPrefab;

    private readonly List<GoalRowView> _rows = new();
    private readonly Stack<GoalRowView> _animalGoalRowPool = new();
    private readonly Stack<GoalRowView> _primaryGoalRowPool = new();
    private readonly Dictionary<GoalRowView, bool> _isPrimaryGoalRow = new();

    public BoardViewGoalRowsPresenter(Transform goalRowsParent, GoalRowView animalGoalRowPrefab, GoalRowView primaryGoalRowPrefab)
    {
        _goalRowsParent = goalRowsParent;
        _animalGoalRowPrefab = animalGoalRowPrefab;
        _primaryGoalRowPrefab = primaryGoalRowPrefab;
    }

    public void Show(bool show)
    {
        if (_goalRowsParent != null)
            _goalRowsParent.gameObject.SetActive(show);
    }

    public void SetScore(int points, int totalPoints)
    {
        ApplyGoalRows(new List<GoalRowSpec>
        {
            new GoalRowSpec
            {
                IsPrimary = true,
                Icon = null,
                Text = $"Points: {points}/{totalPoints}",
                Color = Color.white,
                IsComplete = points >= totalPoints
            }
        });
    }

    public void SetMatchedAnimals(int animals, int goal)
    {
        int remaining = Mathf.Max(0, goal - animals);

        ApplyGoalRows(new List<GoalRowSpec>
        {
            new GoalRowSpec
            {
                IsPrimary = true,
                Icon = null,
                Text = $"Matches: \n{remaining}",
                Color = Color.white,
                IsComplete = animals >= goal
            }
        });
    }

    public void SetCollectGoals(List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        var specs = new List<GoalRowSpec>();

        foreach (var g in goals)
        {
            if (g.animal == null)
                continue;

            collected.TryGetValue(g.animal._id, out int have);
            int remaining = Mathf.Max(0, g.amount - have);
            bool isComplete = have >= g.amount;

            specs.Add(new GoalRowSpec
            {
                IsPrimary = false,
                Icon = g.animal._sprite,
                Text = remaining.ToString(),
                Color = g.animal.color,
                IsComplete = isComplete
            });
        }

        ApplyGoalRows(specs);
    }

    // For level 10
    public void SetPointsAndCollectGoals(int points, int pointsGoal, List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        var specs = new List<GoalRowSpec>
        {
            new GoalRowSpec
            {
                IsPrimary = true,
                Icon = null,
                Text = $"Points: {points}/{pointsGoal}",
                Color = Color.white,
                IsComplete = points >= pointsGoal
            }
        };

        AppendCollectGoalRows(specs, goals, collected);
        ApplyGoalRows(specs);
    }

    public void SetMatchesAndCollectGoals(int matched, int matchGoal, List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        int remainingMatches = Mathf.Max(0, matchGoal - matched);

        var specs = new List<GoalRowSpec>
        {
            new GoalRowSpec
            {
                IsPrimary = true,
                Icon = null,
                Text = remainingMatches.ToString(),
                Color = Color.white,
                IsComplete = matched >= matchGoal
            }
        };

        AppendCollectGoalRows(specs, goals, collected);
        ApplyGoalRows(specs);
    }

    private void AppendCollectGoalRows(List<GoalRowSpec> specs, List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        foreach (var g in goals)
        {
            if (g.animal == null)
                continue;

            collected.TryGetValue(g.animal._id, out int have);
            int remaining = Mathf.Max(0, g.amount - have);
            bool isComplete = have >= g.amount;

            specs.Add(new GoalRowSpec
            {
                IsPrimary = false,
                Icon = g.animal._sprite,
                Text = remaining.ToString(),
                Color = g.animal.color,
                IsComplete = isComplete
            });
        }
    }

    private void AddGoalRow(GoalRowView prefab, Sprite icon, string text, Color color, bool isComplete = false)
    {
        var row = GetGoalRow(prefab, prefab == _primaryGoalRowPrefab);
        row.transform.SetParent(_goalRowsParent, false);
        row.gameObject.SetActive(true);

        row.Set(icon, text, color, isComplete);
        _rows.Add(row);
    }

    private void AddPrimaryGoalRow(string text, bool isComplete = false)
    {
        AddGoalRow(_primaryGoalRowPrefab, null, text, Color.white, isComplete);
    }

    private void AddAnimalGoalRow(Sprite icon, string text, Color color, bool isComplete = false)
    {
        AddGoalRow(_animalGoalRowPrefab, icon, text, color, isComplete);
    }

    private void ApplyGoalRows(List<GoalRowSpec> specs)
    {
        if (!GoalLayoutMatches(specs))
        {
            RebuildGoalRows(specs);
            return;
        }

        for (int i = 0; i < specs.Count; i++)
        {
            var row = _rows[i];
            var spec = specs[i];
            row.Set(spec.Icon, spec.Text, spec.Color, spec.IsComplete);
        }
    }

    private bool GoalLayoutMatches(List<GoalRowSpec> specs)
    {
        if (_rows.Count != specs.Count)
            return false;

        for (int i = 0; i < specs.Count; i++)
        {
            if (_rows[i] == null)
                return false;

            bool rowIsPrimary = _isPrimaryGoalRow.TryGetValue(_rows[i], out bool cachedIsPrimary) && cachedIsPrimary;
            if (rowIsPrimary != specs[i].IsPrimary)
                return false;
        }

        return true;
    }

    private void RebuildGoalRows(List<GoalRowSpec> specs)
    {
        ClearGoalRows();

        for (int i = 0; i < specs.Count; i++)
        {
            var spec = specs[i];
            var prefab = spec.IsPrimary ? _primaryGoalRowPrefab : _animalGoalRowPrefab;
            var row = GetGoalRow(prefab, spec.IsPrimary);
            row.Set(spec.Icon, spec.Text, spec.Color, spec.IsComplete);
            _rows.Add(row);
        }
    }

    private void ClearGoalRows()
    {
        for (int i = 0; i < _rows.Count; i++)
            ReleaseGoalRow(_rows[i]);

        _rows.Clear();
    }

    private GoalRowView GetGoalRow(GoalRowView prefab, bool isPrimary)
    {
        var pool = isPrimary ? _primaryGoalRowPool : _animalGoalRowPool;
        GoalRowView row;

        if (pool.Count > 0)
        {
            row = pool.Pop();
        }
        else
        {
            row = Object.Instantiate(prefab, _goalRowsParent);
        }

        row.transform.SetParent(_goalRowsParent, false);
        row.gameObject.SetActive(true);
        _isPrimaryGoalRow[row] = isPrimary;
        return row;
    }

    private void ReleaseGoalRow(GoalRowView row)
    {
        if (row == null)
            return;

        row.gameObject.SetActive(false);

        bool isPrimary = _isPrimaryGoalRow.TryGetValue(row, out bool cachedIsPrimary) && cachedIsPrimary;
        if (isPrimary)
            _primaryGoalRowPool.Push(row);
        else
            _animalGoalRowPool.Push(row);
    }
}
