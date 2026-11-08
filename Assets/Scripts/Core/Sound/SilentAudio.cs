/// <summary>Plays nothing. For tests and tools that run gameplay without sound.</summary>
public sealed class SilentAudio : IGameAudio
{
    public static readonly SilentAudio Instance = new SilentAudio();

    public void PlaySfx(SfxId sfx, float volume = 1f) { }
    public void PlaySfxVaried(SfxId sfx, float volume = 1f) { }
    public void PlayTileBreak() { }
    public void PlayLevelMusic(SongName song) { }
    public void PlayTimerMusic() { }
    public void SetMusicVolume(float volume) { }
}
