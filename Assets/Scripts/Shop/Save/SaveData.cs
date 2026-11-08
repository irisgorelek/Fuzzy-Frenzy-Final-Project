using System;
using System.Collections.Generic;

[Serializable]
internal class SaveData
{
    public string playerId;
    public string playerName;
    public int coins;
    public int extraMoveCount;
    public List<BoosterEntry> boosters = new List<BoosterEntry>();
    public int maxLives;
    public int currentLives;
    public long lastLifeTimestampUtcSeconds;
    public List<string> unlockedAchievements = new();
    public List<int> completedLevels = new();
    public List<string> discoveredAnimals = new();
    public List<DestroyedAnimalEntry> destroyedAnimals = new();
    public int totalDestroyedAnimals;
    public int totalPointsEarned;
    public List<AvatarSelectionEntry> avatarSelections = new();
    public List<LevelStarEntry> levelStars = new();
    public List<string> unlockedAvatarItems = new();
    public string equippedAccessoryId;
    public List<LevelScoreEntry> levelBestScores = new();
    public List<int> claimedRewards = new();
}
