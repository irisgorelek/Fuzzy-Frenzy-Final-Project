using System;
using System.Collections.Generic;
using UnityEngine;

public static class PlayerEconomyStorage
{
    private const string Key = "PLAYER_ECONOMY_V1";

    [Serializable]
    private class BoosterEntry
    {
        public BoosterEffectType type;
        public int count;
    }

    [Serializable]
    private class DestroyedAnimalEntry
    {
        public string animalId;
        public int count;
    }

    [Serializable]
    private class AvatarSelectionEntry
    {
        public AvatarCategoryType category;
        public int index;
    }

    [Serializable]
    private class SaveData
    {
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
    }

    public static void Save(PlayerEconomyState state)
    {
        var data = new SaveData
        {
            coins = state.coins,
            extraMoveCount = state.extraMoveCount,
            maxLives = state.maxLives,
            currentLives = state.currentLives,
            lastLifeTimestampUtcSeconds = state.lastLifeTimestampUtcSeconds
        };

        foreach (var kvp in state.boosters)
        {
            data.boosters.Add(new BoosterEntry { type = kvp.Key, count = kvp.Value });
        }

        data.unlockedAchievements = new List<string>(state.unlockedAchievements);
        data.completedLevels = new List<int>(state.completedLevels);
        data.discoveredAnimals = new List<string>(state.discoveredAnimals);
        data.totalDestroyedAnimals = state.totalDestroyedAnimals;
        data.totalPointsEarned = state.totalPointsEarned;

        foreach (var kvp in state.destroyedAnimals)
            data.destroyedAnimals.Add(new DestroyedAnimalEntry { animalId = kvp.Key, count = kvp.Value });

        foreach (var kvp in state.avatarSelections)
            data.avatarSelections.Add(new AvatarSelectionEntry { category = kvp.Key, index = kvp.Value });

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(Key, json);
        PlayerPrefs.Save();
    }

    public static PlayerEconomyState LoadOrCreate()
    {
        var state = new PlayerEconomyState();

        if (!PlayerPrefs.HasKey(Key))                   // safetey check key empty
            return state;

        string json = PlayerPrefs.GetString(Key);
        if (string.IsNullOrEmpty(json))                 // safetey check string empty
            return state;

        try
        {
            var data = JsonUtility.FromJson<SaveData>(json);
            state.coins = data.coins;
            // maxLives: if missing in old save, default to 3
            state.maxLives = (data.maxLives <= 0) ? 3 : data.maxLives;

            // currentLives: allow 0 as valid!
            // If field missing in old saves, JsonUtility will also give 0,
            // so we need a better signal.
            // We'll treat "missing old save" as: lastLifeTimestampUtcSeconds == 0 AND currentLives == 0
            bool looksLikeOldSave = (data.lastLifeTimestampUtcSeconds == 0 && data.currentLives == 0);

            state.currentLives = looksLikeOldSave ? state.maxLives : Mathf.Clamp(data.currentLives, 0, state.maxLives);
            state.lastLifeTimestampUtcSeconds = data.lastLifeTimestampUtcSeconds;
            state.extraMoveCount = data.extraMoveCount;

            state.boosters.Clear();
            if (data.boosters != null)
            {
                foreach (var entry in data.boosters)
                    state.boosters[entry.type] = entry.count;
            }

            if (data.unlockedAchievements != null)
                state.unlockedAchievements = new HashSet<string>(data.unlockedAchievements);
            if (data.completedLevels != null)
                state.completedLevels = new HashSet<int>(data.completedLevels);
            if (data.discoveredAnimals != null)
                state.discoveredAnimals = new HashSet<string>(data.discoveredAnimals);

            state.totalDestroyedAnimals = data.totalDestroyedAnimals;
            state.totalPointsEarned = data.totalPointsEarned;

            state.destroyedAnimals.Clear();
            if (data.destroyedAnimals != null)
            {
                foreach (var entry in data.destroyedAnimals)
                    state.destroyedAnimals[entry.animalId] = entry.count;
            }

            state.avatarSelections.Clear();
            if (data.avatarSelections != null)
            {
                foreach (var entry in data.avatarSelections)
                    state.avatarSelections[entry.category] = entry.index;
            }
        }
        catch
        {
            // If corrupted, return a fresh state
            return new PlayerEconomyState();
        }

        return state;
    }

    public static void Wipe()
    {
        PlayerPrefs.DeleteKey(Key);
    }
}
