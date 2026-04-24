using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string levelSceneName = "Level";
    //void Awake()
    //{
    //    QualitySettings.vSyncCount = 0;   // ignored on most mobile platforms, but harmless
    //    Application.targetFrameRate = 60;
    //}
    private void Start()
    {
        AudioManager.instance.PlayTitle(); // Play the title music
    }
    public void LoadLevel()
    {
        SceneManager.LoadScene(levelSceneName);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu+Shop");
    }
}
