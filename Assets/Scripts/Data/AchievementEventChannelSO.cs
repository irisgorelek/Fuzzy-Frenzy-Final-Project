using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AchievementEventChannelSO", menuName = "Scriptable Objects/Channels/AchievementEventChannelSO")]
public class AchievementEventChannelSO : ScriptableObject
{
    public event Action<AchievementSO> OnEventRaised;

    public void RaiseEvent(AchievementSO achievement)
    {
        OnEventRaised?.Invoke(achievement);
    }
}
