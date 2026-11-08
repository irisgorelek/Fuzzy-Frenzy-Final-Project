using UnityEngine;

/// <summary>Scene objects and services the end of a level uses. Filled from BoardController's Inspector fields.</summary>
public sealed class LevelOutcomeServices
{
    public LevelClearedPopupUI ClearedPopup;
    public GameObject ClearedDimmer;
    public LevelLostPopupUI LostPopup;
    public LevelVFXToggle VfxToggle;
    public RewardsConfig Rewards;
    public BootstrapperLocator Locator;
    public LevelCompletedEventChannelSO LevelCompletedChannel;
    public IGameAudio Audio;
}
