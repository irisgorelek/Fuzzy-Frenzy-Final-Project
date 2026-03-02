using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelEndButtons : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button redoButton;
    [SerializeField] private LevelsData allLevels;

    private GameBootstrapper _bootstrapper;

    private void Start()
    {
        _bootstrapper = FindFirstObjectByType<GameBootstrapper>();

        menuButton.onClick.AddListener(GoToMenu);
        redoButton.onClick.AddListener(RedoLevel);
        nextLevelButton.onClick.AddListener(GoToNextLevel);

        // Hide "Next Level" if this is the last level
        var current = _bootstrapper.SelectedLevel;
        int currentIndex = allLevels.Levels.IndexOf(current);
        if (currentIndex < 0 || currentIndex >= allLevels.Levels.Count - 1)
            nextLevelButton.gameObject.SetActive(false);
    }

    private void GoToMenu()
    {
        SceneManager.LoadScene("MainMenu+Shop");
    }

    private void RedoLevel()
    {
        SceneManager.LoadScene("Level");
    }

    private void GoToNextLevel()
    {
        var current = _bootstrapper.SelectedLevel;
        int currentIndex = allLevels.Levels.IndexOf(current);
        _bootstrapper.SelectedLevel = allLevels.Levels[currentIndex + 1];
        SceneManager.LoadScene("Level");
    }
}
