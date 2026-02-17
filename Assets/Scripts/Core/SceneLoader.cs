using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string levelSceneName = "Level";

    public void LoadLevel()
    {
        SceneManager.LoadScene(levelSceneName);
    }

    public void LoadMainMenu()
    {
        SceneManager.LoadScene("MainMenu+Shop");
    }
}
