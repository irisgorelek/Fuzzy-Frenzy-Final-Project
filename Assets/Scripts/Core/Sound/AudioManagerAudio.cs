/// <summary>Plays through the scene's AudioManager. Does nothing while there isn't one, for example in a test scene.</summary>
public sealed class AudioManagerAudio : IGameAudio
{
    public static readonly AudioManagerAudio Instance = new AudioManagerAudio();

    private static AudioManager Manager => AudioManager.instance;

    public void PlaySfx(SfxId sfx, float volume = 1f)
    {
        if (Manager != null) Manager.PlaySFX((int)sfx, volume);
    }

    public void PlaySfxVaried(SfxId sfx, float volume = 1f)
    {
        if (Manager != null) Manager.PlaySFXPitchAdjusted((int)sfx, volume);
    }

    public void PlayTileBreak()
    {
        if (Manager != null) Manager.PlayBoneBreak();
    }

    public void PlayLevelMusic(SongName song)
    {
        if (Manager != null) Manager.PlayBG((int)song);
    }

    public void PlayTimerMusic()
    {
        if (Manager != null) Manager.PlayTimerMusic();
    }

    public void SetMusicVolume(float volume)
    {
        if (Manager != null) Manager.ChangeMusicVolume(volume);
    }
}
