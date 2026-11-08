using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct TriggeredSpeechEntry
{
    public int levelIndex;
    public Animal triggerAnimal;
    [Tooltip("Optional. If empty, a random animal on the board is chosen as the speaker.")]
    public Animal speakerAnimal;
    [TextArea(1, 3)] public List<string> lines; // random pick one
}
