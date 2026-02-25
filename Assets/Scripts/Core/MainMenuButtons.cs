using UnityEngine;
using UnityEngine.UI;

public class MainMenuButtons : MonoBehaviour
{
    // Settings
    public void OpenSettingsPanel(GameObject panel)
    {
        panel.SetActive(true);
    }

    public void CloseSettingsPanel(GameObject panel)
    {
        panel.SetActive(false);
    }
    public void PlayButtonSound(int sound = 3)
    {
        AudioManager.instance.PlaySFXPitchAdjusted(sound, 0.5f);
    }
}
