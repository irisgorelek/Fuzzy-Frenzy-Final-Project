using System;
using UnityEngine;

[CreateAssetMenu(fileName = "VoidEventChannelSO", menuName = "Scriptable Objects/Channels/VoidEventChannelSO")]
public class VoidEventChannelSO : ScriptableObject
{
    public event Action OnEventRaised;

    public void RaiseEvent()
    {
        OnEventRaised?.Invoke();
    }
}
