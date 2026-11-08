using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Draws the level's HUD: goal rows (pooled), the moves counter, the timer, the level number and background.
/// Knows nothing about the board or the rules; it shows the numbers it is given.
/// </summary>
public sealed class BoardHud : IBoardHud
{
    private readonly BoardHudParts _parts;
    private readonly List<GoalRowView> _rows = new List<GoalRowView>();
    private readonly Stack<GoalRowView> _animalGoalRowPool = new Stack<GoalRowView>();
    private readonly Stack<GoalRowView> _primaryGoalRowPool = new Stack<GoalRowView>();
    private readonly Dictionary<GoalRowView, bool> _isPrimaryGoalRow = new Dictionary<GoalRowView, bool>();
    private readonly List<GoalRowSpec> _specs = new List<GoalRowSpec>();
    private Tween _lastMoveShakeTween;

    public BoardHud(BoardHudParts parts)
    {
        _parts = parts;
    }

    // ---- Level ----

    public void SetLevelNumber(int level)
    {
        _parts.LevelNumberText.text = level.ToString();
    }

    public void SetBackground(Sprite backgroundSprite)
    {
        if (_parts.BackgroundImage == null)
        {
            Debug.LogWarning("BoardView: background image is missing.");
            return;
        }

        _parts.BackgroundImage.sprite = backgroundSprite;
    }

    // ---- Goals ----

    public void ShowGoal(bool show)
    {
        if (_parts.GoalRowsParent != null)
            _parts.GoalRowsParent.gameObject.SetActive(show);
    }

    public void SetScore(int points, int totalPoints)
    {
        _specs.Clear();
        _specs.Add(PointsRow(points, totalPoints));
        ApplyGoalRows(_specs);
    }

    public void SetMatchedAnimals(int animals, int goal)
    {
        int remaining = Mathf.Max(0, goal - animals);

        _specs.Clear();
        _specs.Add(new GoalRowSpec
        {
            IsPrimary = true,
            Icon = null,
            Text = $"Matches: \n{remaining}",
            AnimateCounter = true,
            Color = Color.white,
            IsComplete = animals >= goal
        });
        ApplyGoalRows(_specs);
    }

    public void SetCollectGoals(List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected)
    {
        _specs.Clear();
        AddCollectRows(goals, collected);
        ApplyGoalRows(_specs);
    }

    // For level 10
    public void SetPointsAndCollectGoals(int points, int pointsGoal, List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected)
    {
        _specs.Clear();
        _specs.Add(PointsRow(points, pointsGoal));
        AddCollectRows(goals, collected);
        ApplyGoalRows(_specs);
    }

    /// <summary>The goal row showing this animal, for the collect animation to fly to.</summary>
    public GoalRowView FindAnimalGoal(Sprite sprite)
    {
        foreach (var row in _rows)
            if (row != null && row.CanCollect && row.IconSprite == sprite) return row;
        return null;
    }

    // ---- Moves ----

    public void SetMovesText(int movesLeft)
    {
        if (_parts.MovesCountText != null)
            _parts.MovesCountText.text = movesLeft.ToString();
    }

    public void SetMovesLastMoveTension(bool active)
    {
        if (_parts.MovesCountText == null)
            return;

        var rt = _parts.MovesCountText.rectTransform;
        rt.DOKill();
        _lastMoveShakeTween?.Kill();
        _lastMoveShakeTween = null;

        if (!active)
        {
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
            return;
        }

        _lastMoveShakeTween = rt
            .DOShakeAnchorPos(0.35f, strength: 18f, vibrato: 40, randomness: 90f, snapping: false)
            .SetLoops(-1, LoopType.Restart);
    }

    // ---- Timer ----

    public void SetTimerVisible(bool visible)
    {
        if (_parts.TimerText != null)
            _parts.TimerText.gameObject.SetActive(visible);
        if (_parts.TimerBackground != null)
            _parts.TimerBackground.gameObject.SetActive(visible);
    }

    public void SetTimerSeconds(int seconds)
    {
        if (_parts.TimerText != null)
            _parts.TimerText.text = $"Match Time!\n{seconds}";
    }

    // ---- Goal rows ----

    private static GoalRowSpec PointsRow(int points, int goal) => new GoalRowSpec
    {
        IsPrimary = true,
        Icon = null,
        Text = $"Points: {points}/{goal}",
        Color = Color.white,
        IsComplete = points >= goal
    };

    private void AddCollectRows(List<AnimalGoal> goals, IReadOnlyDictionary<string, int> collected)
    {
        foreach (var g in goals)
        {
            if (g.animal == null)
                continue;

            collected.TryGetValue(g.animal._id, out int have);

            _specs.Add(new GoalRowSpec
            {
                IsPrimary = false,
                Icon = g.animal._sprite,
                Text = Mathf.Max(0, g.amount - have).ToString(),
                Color = g.animal.color,
                IsComplete = have >= g.amount
            });
        }
    }

    private void ApplyGoalRows(List<GoalRowSpec> specs)
    {
        if (!GoalLayoutMatches(specs))
            RebuildGoalRows(specs);

        for (int i = 0; i < specs.Count; i++)
        {
            var spec = specs[i];
            var row = _rows[i];
            row.transform.SetSiblingIndex(i);
            row.Set(spec.Icon, spec.Text, spec.Color, spec.IsComplete, spec.AnimateCounter);
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
        for (int i = 0; i < _rows.Count; i++)
            ReleaseGoalRow(_rows[i]);
        _rows.Clear();

        for (int i = 0; i < specs.Count; i++)
        {
            var spec = specs[i];
            var prefab = spec.IsPrimary ? _parts.PrimaryGoalRowPrefab : _parts.AnimalGoalRowPrefab;
            var row = GetGoalRow(prefab, spec.IsPrimary);
            row.transform.SetSiblingIndex(i);
            _rows.Add(row);
        }
    }

    private GoalRowView GetGoalRow(GoalRowView prefab, bool isPrimary)
    {
        var pool = isPrimary ? _primaryGoalRowPool : _animalGoalRowPool;
        var row = pool.Count > 0 ? pool.Pop() : Object.Instantiate(prefab, _parts.GoalRowsParent);

        row.transform.SetParent(_parts.GoalRowsParent, false);
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
