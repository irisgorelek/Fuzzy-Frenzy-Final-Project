using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string levelSceneName = "Level";
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
