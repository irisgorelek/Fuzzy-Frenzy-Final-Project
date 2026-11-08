/// <summary>
/// The sounds gameplay code asks for. Level code takes this instead of reaching for AudioManager.instance,
/// so it can run in tests with <see cref="SilentAudio"/>.
/// </summary>
public interface IGameAudio
{
    void PlaySfx(SfxId sfx, float volume = 1f);

    /// <summary>Plays with a small random pitch change, for sounds that repeat a lot.</summary>
    void PlaySfxVaried(SfxId sfx, float volume = 1f);

    /// <summary>The breaking sound for an obstacle tile.</summary>
    void PlayTileBreak();

    void PlayLevelMusic(SongName song);
    void PlayTimerMusic();
    void SetMusicVolume(float volume);
}
