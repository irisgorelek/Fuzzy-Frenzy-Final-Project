using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct NormalSpeechEntry
{
    public Animal animal;
    [TextArea(1, 3)] public List<string> lines; // random pick one
}
