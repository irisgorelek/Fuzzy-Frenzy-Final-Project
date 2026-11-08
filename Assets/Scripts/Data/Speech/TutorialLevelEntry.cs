using System;
using System.Collections.Generic;

[Serializable]
public struct TutorialLevelEntry
{
    public int levelIndex;
    public List<TutorialSpeechStep> steps;
}
