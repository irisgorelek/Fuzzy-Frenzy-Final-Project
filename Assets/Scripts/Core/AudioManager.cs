using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.LowLevelPhysics;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [Header("Music Sources")]
    [SerializeField] private AudioSource _titleMusic;
    [SerializeField] private AudioSource _timerMusic;
    [SerializeField] private AudioSource[] _bg;

    [Header("SFX Sources")]
    [SerializeField] private AudioSource[] _sfx;

    [Header("Crossfade")]
    [SerializeField] private float _musicTargetVolume = 0.6f;
    [SerializeField] private float _musicFadeSeconds = 0.8f;

    private AudioSource _currentMusic;
    private CancellationTokenSource _musicFadeCts;


    private const string MusicPref = "MusicOn";
    private const string SfxPref = "SfxOn";

    public bool MusicOn { get; private set; } = true;
    public bool SfxOn { get; private set; } = true;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Load saved settings
        MusicOn = PlayerPrefs.GetInt(MusicPref, 1) == 1;
        SfxOn = PlayerPrefs.GetInt(SfxPref, 1) == 1;

        ApplyMusicMute();
        ApplySfxMute();
    }

    public void SetMusicOn(bool on)
    {
        if (on == true) Debug.Log("Unmuted Music");
        else Debug.Log("Muted Music");

        MusicOn = on;
        PlayerPrefs.SetInt(MusicPref, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicMute();
    }

    public void SetSfxOn(bool on)
    {
        if (on == true) Debug.Log("Unmuted SFX");
        else Debug.Log("Muted SFX");
        SfxOn = on;
        PlayerPrefs.SetInt(SfxPref, on ? 1 : 0);
        PlayerPrefs.Save();
        ApplySfxMute();
    }

    public void SetMusicMuted(bool muted) => SetMusicOn(!muted);
    public void SetSfxMuted(bool muted) => SetSfxOn(!muted);

    private void ApplyMusicMute()
    {
        bool muted = !MusicOn;

        if (_titleMusic) _titleMusic.mute = muted;
        if (_timerMusic) _timerMusic.mute = muted;

        foreach (var song in _bg)
            if (song) song.mute = muted;
    }

    private void ApplySfxMute()
    {
        bool muted = !SfxOn;

        foreach (var s in _sfx)
            if (s) s.mute = muted;
    }
    public void ChangeMusicVolume(float volume = 0.4f)
    {
        GetCurrentMusic().volume = volume;
    }

    public void PlayTitle() => CrossFadeMusic(_titleMusic);

    public void PlaySFX(int sfxToPlay, float volume = 1f)
    {
        if (!SfxOn) return; // Prevents starting sounds while "off"
        if (sfxToPlay < 0 || sfxToPlay >= _sfx.Length) return;

        _sfx[sfxToPlay].volume = volume;

        _sfx[sfxToPlay].Stop();
        _sfx[sfxToPlay].Play();
    }

    public void PlaySFXPitchAdjusted(int sfxToPlay, float volume = 1f)
    {
        if (!SfxOn) return;
        if (sfxToPlay < 0 || sfxToPlay >= _sfx.Length) return;

        _sfx[sfxToPlay].pitch = Random.Range(0.8f, 1.2f);
        PlaySFX(sfxToPlay, volume);
    }

    public void PlayBG(int bgToPlay)
    {
        if (bgToPlay < 0 || bgToPlay >= _bg.Length) return;

        CrossFadeMusic(_bg[bgToPlay]);
    }
    
    public void PlayTimerMusic()
    {
        CrossFadeMusic(_timerMusic);
    }

    private void CrossFadeMusic(AudioSource next)
    {
        if (next == null) return;
        if (_currentMusic == next && next.isPlaying) return;

        _musicFadeCts?.Cancel();
        _musicFadeCts?.Dispose();
        _musicFadeCts = new CancellationTokenSource();

        _ = CrossFadeRoutineAsync(_currentMusic, next, _musicFadeSeconds, _musicFadeCts.Token);
        _currentMusic = next;
    }

    private async Task CrossFadeRoutineAsync(AudioSource from, AudioSource to, float seconds, CancellationToken ct)
    {
        to.Stop();
        to.volume = 0f;
        to.mute = !MusicOn;
        to.Play();

        float t = 0f;
        float fromStart = from ? from.volume : 0f;
        float toTarget = MusicOn ? _musicTargetVolume : 0f;

        while (t < seconds)
        {
            ct.ThrowIfCancellationRequested();

            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / seconds);

            if (from) from.volume = Mathf.Lerp(fromStart, 0f, k);
            to.volume = Mathf.Lerp(0f, toTarget, k);

            await Task.Yield(); // next frame
        }

        if (from)
        {
            from.volume = 0f;
            from.Stop();
        }
        to.volume = toTarget;
    }
    private AudioSource GetCurrentMusic()
    {
        if (_timerMusic && _timerMusic.isPlaying) return _timerMusic;
        if (_titleMusic && _titleMusic.isPlaying) return _titleMusic;

        foreach (var bg in _bg)
            if (bg && bg.isPlaying) return bg;

        return null;
    }
}