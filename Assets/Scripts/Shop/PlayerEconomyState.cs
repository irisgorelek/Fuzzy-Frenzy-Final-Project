using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PlayerEconomyState
{
    public int coins = 0;
    public int extraMoveCount = 0;
    public int maxLives = 3;
    public int currentLives = 3;

    // Achievement tracking
    public HashSet<string> unlockedAchievements = new();
    public HashSet<int> completedLevels = new();
    public HashSet<string> discoveredAnimals = new();
    public Dictionary<string, int> destroyedAnimals = new();
    public int totalDestroyedAnimals = 0;
    public int totalPointsEarned = 0;

    // Avatar selections (index per category)
    public Dictionary<AvatarCategoryType, int> avatarSelections = new();

    // When was regen last accounted for
    public long lastLifeTimestampUtcSeconds = 0;

    // Booster counts by effect type
    public Dictionary<BoosterEffectType, int> boosters = new Dictionary<BoosterEffectType, int>();      // current boosters

    public int GetBoosterCount(BoosterEffectType type)              // return current boosters
    {
        return boosters.TryGetValue(type, out int count) ? count : 0;
    }

    public void AddBooster(BoosterEffectType type, int amount)      // add boosters to dictionary
    {
        if (amount <= 0) return;
        boosters[type] = GetBoosterCount(type) + amount;
    }

    public bool SpendCoins(int amount)
    {
        if (amount < 0) return false;
        if (coins < amount) return false;
        coins -= amount;
        return true;
    }

    public void AddCoins(int amount)
    {
        if (amount <= 0) return;
        coins += amount;
    }

    public void AddExtraMoves(int amount)
    {
        if (amount <= 0) return;
        extraMoveCount += amount;
    }
}
