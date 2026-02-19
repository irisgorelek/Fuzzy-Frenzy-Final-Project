using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PowerUpEventChannelSO", menuName = "Scriptable Objects/Channels/PowerUpEventChannelSO")]
public class PowerUpEventChannelSO : ScriptableObject
{
    public event Action<string> OnEventRaised;

    public void RaiseEvent(string powerUpName)
    {
        OnEventRaised.Invoke(powerUpName);
    }
}
