using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public sealed class BoardViewHud
{
    private readonly TextMeshProUGUI _levelNumberText;
    private readonly Image _backgroundImage;
    private readonly TextMeshProUGUI _movesCountText;
    private readonly TextMeshProUGUI _timerPowerUp;
    private readonly Image _timerBackground;
    private readonly BoardViewGoalRowsPresenter _goalRowsPresenter;
    private Tween _lastMoveShakeTween;

    public BoardViewHud(
        TextMeshProUGUI levelNumberText,
        Image backgroundImage,
        TextMeshProUGUI movesCountText,
        TextMeshProUGUI timerPowerUp,
        Image timerBackground,
        BoardViewGoalRowsPresenter goalRowsPresenter)
    {
        _levelNumberText = levelNumberText;
        _backgroundImage = backgroundImage;
        _movesCountText = movesCountText;
        _timerPowerUp = timerPowerUp;
        _timerBackground = timerBackground;
        _goalRowsPresenter = goalRowsPresenter;
    }

    public void SetLevelNumber(int level)
    {
        if (_levelNumberText != null)
            _levelNumberText.text = level.ToString();
    }

    public void SetBackground(Sprite backgroundSprite)
    {
        if (_backgroundImage == null)
        {
            Debug.LogWarning("BoardView: background image is missing.");
            return;
        }

        _backgroundImage.sprite = backgroundSprite;
    }

    public void ShowGoal(bool show)
    {
        _goalRowsPresenter.Show(show);
    }

    public void SetScore(int points, int totalPoints)
    {
        _goalRowsPresenter.SetScore(points, totalPoints);
    }

    public void SetMatchedAnimals(int animals, int goal)
    {
        _goalRowsPresenter.SetMatchedAnimals(animals, goal);
    }

    public void SetCollectGoals(List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        _goalRowsPresenter.SetCollectGoals(goals, collected);
    }

    // For level 10
    public void SetPointsAndCollectGoals(int points, int pointsGoal, List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        _goalRowsPresenter.SetPointsAndCollectGoals(points, pointsGoal, goals, collected);
    }

    public void SetMatchesAndCollectGoals(int matched, int matchGoal, List<AnimalGoal> goals, Dictionary<string, int> collected)
    {
        _goalRowsPresenter.SetMatchesAndCollectGoals(matched, matchGoal, goals, collected);
    }

    public void SetMovesText(int movesLeft)
    {
        if (_movesCountText != null)
            _movesCountText.text = movesLeft.ToString();
    }

    public void SetMovesLastMoveTension(bool active)
    {
        if (_movesCountText == null)
            return;

        var rt = _movesCountText.rectTransform;
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

    public void SetTimerVisible(bool visible)
    {
        if (_timerPowerUp != null)
            _timerPowerUp.gameObject.SetActive(visible);
        if (_timerBackground != null)
            _timerBackground.gameObject.SetActive(visible);
    }

    public void SetTimerSeconds(int seconds)
    {
        if (_timerPowerUp != null)
            _timerPowerUp.text = $"Match Time!\n{seconds}";
    }
}
