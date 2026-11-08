using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct TutorialSpeechStep
{
    public Animal animal;
    public SpeechSide side;
    [TextArea(2, 5)] public List<string> lines;
}
