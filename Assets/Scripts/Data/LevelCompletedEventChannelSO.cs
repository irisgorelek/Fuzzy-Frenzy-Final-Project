using System;
using UnityEngine;

[CreateAssetMenu(fileName = "LevelCompletedEventChannelSO", menuName = "Scriptable Objects/Channels/LevelCompletedEventChannelSO")]
public class LevelCompletedEventChannelSO : ScriptableObject
{
    public event Action<int> OnEventRaised;

    public void RaiseEvent(int levelId)
    {
        OnEventRaised?.Invoke(levelId);
    }
}
