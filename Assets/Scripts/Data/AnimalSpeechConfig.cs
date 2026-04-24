using System;
using System.Collections.Generic;
using UnityEngine;

public enum SpeechSide
{
    Left,
    Right
}

[CreateAssetMenu(fileName = "AnimalSpeechConfig", menuName = "Scriptable Objects/AnimalSpeechConfig")]
public class AnimalSpeechConfig : ScriptableObject
{
    [Header("Tutorial Speech")]
    public List<TutorialLevelEntry> tutorialLevels = new();

    [Header("Normal Speech")]
    public List<NormalSpeechEntry> normalEntries = new();

    [Header("Triggered Speech")]
    public List<TriggeredSpeechEntry> triggeredEntries = new();

    public bool TryGetTutorialLevel(int levelIndex, out TutorialLevelEntry entry)
    {
        for (int i = 0; i < tutorialLevels.Count; i++)
        {
            var candidate = tutorialLevels[i];
            if (candidate.levelIndex == levelIndex && candidate.steps != null && candidate.steps.Count > 0)
            {
                entry = candidate;
                return true;
            }
        }

        entry = default;
        return false;
    }

    public bool TryGetRandomNormalLines(Animal animal, out List<string> lines)
    {
        lines = null;
        if (animal == null)
            return false;

        for (int i = 0; i < normalEntries.Count; i++)
        {
            var entry = normalEntries[i];
            if (entry.animal != animal || entry.lines == null || entry.lines.Count == 0)
                continue;

            lines = new List<string>();
            for (int j = 0; j < entry.lines.Count; j++)
            {
                if (!string.IsNullOrWhiteSpace(entry.lines[j]))
                    lines.Add(entry.lines[j]);
            }

            return lines.Count > 0;
        }

        return false;
    }

    public List<TriggeredSpeechEntry> GetTriggeredEntriesForLevel(int levelIndex)
    {
        var result = new List<TriggeredSpeechEntry>();
        for (int i = 0; i < triggeredEntries.Count; i++)
        {
            if (triggeredEntries[i].levelIndex == levelIndex && triggeredEntries[i].triggerAnimal != null)
                result.Add(triggeredEntries[i]);
        }
        return result;
    }
}

[Serializable]
public struct TutorialLevelEntry
{
    public int levelIndex;
    public List<TutorialSpeechStep> steps;
}

[Serializable]
public struct TutorialSpeechStep
{
    public Animal animal;
    public SpeechSide side;
    [TextArea(2, 5)] public List<string> lines;
}

[Serializable]
public struct NormalSpeechEntry
{
    public Animal animal;
    [TextArea(1, 3)] public List<string> lines; // random pick one
}

[Serializable]
public struct TriggeredSpeechEntry
{
    public int levelIndex;
    public Animal triggerAnimal;
    [Tooltip("Optional. If empty, a random animal on the board is chosen as the speaker.")]
    public Animal speakerAnimal;
    [TextArea(1, 3)] public List<string> lines; // random pick one
}
