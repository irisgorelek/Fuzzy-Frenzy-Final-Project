using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
    [SerializeField] private Toggle musicToggle; 
    [SerializeField] private Toggle sfxToggle;

    private void OnEnable()
    {
        if (musicToggle == null || sfxToggle == null)
        {
            Debug.LogError("SettingsPanel: Toggle reference missing.", this);
            return;
        }

        if (AudioManager.instance == null)
        {
            Debug.LogWarning("SettingsPanel: AudioManager.instance is null on OnEnable. Will try again next frame.", this);
            StartCoroutine(InitNextFrame());
            return;
        }

        SyncTogglesFromAudioManager();
    }

    private System.Collections.IEnumerator InitNextFrame()
    {
        yield return null;
        if (AudioManager.instance != null)
            SyncTogglesFromAudioManager();
    }

    private void SyncTogglesFromAudioManager()
    {
        musicToggle.SetIsOnWithoutNotify(!AudioManager.instance.MusicOn);
        sfxToggle.SetIsOnWithoutNotify(!AudioManager.instance.SfxOn);
    }
}