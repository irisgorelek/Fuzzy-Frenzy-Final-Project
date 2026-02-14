using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Channels/ScoreEventChannelSO")]
public class ScoreEventChannelSO : ScriptableObject
{
    public event Action<int> OnEventRaised;

    public void RaiseEvent(int amount)
    {
        OnEventRaised?.Invoke(amount);
    }
}

