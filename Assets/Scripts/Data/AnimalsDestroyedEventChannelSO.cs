using System;
using UnityEngine;

[CreateAssetMenu(fileName = "AnimalsDestroyedEventChannelSO", menuName = "Scriptable Objects/Channels/AnimalsDestroyedEventChannelSO")]
public class AnimalsDestroyedEventChannelSO : ScriptableObject
{
    public event Action<string, int> OnEventRaised;

    public void RaiseEvent(string animal, int amount)
    {
        OnEventRaised?.Invoke(animal, amount);
    }
}
