using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene objects the HUD draws into. Filled from BoardView's Inspector fields.</summary>
public sealed class BoardHudParts
{
    public TextMeshProUGUI LevelNumberText;
    public Image BackgroundImage;
    public Transform GoalRowsParent;
    public GoalRowView AnimalGoalRowPrefab;
    public GoalRowView PrimaryGoalRowPrefab;
    public TextMeshProUGUI MovesCountText;
    public TextMeshProUGUI TimerText;
    public Image TimerBackground;
}
