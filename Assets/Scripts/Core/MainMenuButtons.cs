using UnityEngine;
using UnityEngine.UI;

public class MainMenuButtons : MonoBehaviour
{
    // Settings
    public void OpenSettingsPanel(GameObject panel)
    {
        //panel.SetActive(true);
        if (panel == null) return;

        var tween = panel.GetComponent<UIPopupTween>();
        if (tween == null)
        {
            Debug.LogError($"No UIPopupTween found on {panel.name}");
            return;
        }

        tween.Show();
    }

    public void CloseSettingsPanel(GameObject panel)
    {
        //panel.SetActive(false);
        if (panel == null) return;

        var tween = panel.GetComponent<UIPopupTween>();
        if (tween == null)
        {
            Debug.LogError($"No UIPopupTween found on {panel.name}");
            return;
        }

        tween.Hide();
    }
    public void PlayButtonSound(int sound = 3)
    {
        AudioManager.instance.PlaySFXPitchAdjusted(sound, 0.5f);
    }
}
