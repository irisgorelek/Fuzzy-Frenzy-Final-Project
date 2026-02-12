using UnityEngine;

[CreateAssetMenu(menuName = "Game/Rewards/Rewards Config", fileName = "RewardsConfig")]
public class RewardsConfig : ScriptableObject
{
    [Header("Stars by moves ratio (movesUsed/maxMoves)")]
    [Range(0.1f, 1f)] public float threeStarThreshold = 0.4f;
    [Range(0.1f, 1f)] public float twoStarThreshold = 0.8f;

    [Header("Base coins by stars")]
    public int coins1Star = 100;
    public int coins2Star = 150;
    public int coins3Star = 200;

    [Header("Level multiplier (x) by levelIndex")]
    public AnimationCurve levelMultiplier = AnimationCurve.Linear(1, 1f, 10, 2f);

    public int GetStars(int maxMoves, int movesUsed)
    {
        if (maxMoves <= 0) return 1;
        float ratio = movesUsed / (float)maxMoves;

        if (ratio <= threeStarThreshold) return 3;
        if (ratio <= twoStarThreshold) return 2;
        return 1;
    }

    public int GetCoins(int stars, int levelIndex)
    {
        int baseCoins = stars == 3 ? coins3Star : stars == 2 ? coins2Star : coins1Star;
        float mult = levelMultiplier.Evaluate(levelIndex);
        return Mathf.RoundToInt(baseCoins * mult);
    }
}
