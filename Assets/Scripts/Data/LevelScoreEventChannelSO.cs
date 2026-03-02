using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Game/Rewards/Level Reward or Score Event Channel")]
public class LevelScoreEventChannelSO : ScriptableObject
{
    public UnityAction<int> OnEventRaised;

    public void RaiseEvent(int delta)
    {
        OnEventRaised?.Invoke(delta);
    }
}