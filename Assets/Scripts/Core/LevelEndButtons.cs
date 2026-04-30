using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelEndButtons : MonoBehaviour
{
    [SerializeField] private Button menuButton;
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button redoButton;
    [SerializeField] private LevelsData allLevels;
    [SerializeField] private GameObject _noLivesWindow;
    [SerializeField] private Button _noLivesWindowXButton;

    private GameBootstrapper _bootstrapper;

    private void Start()
    {
        _bootstrapper = GameBootstrapper.Instance;

        menuButton.onClick.AddListener(GoToMenu);
        redoButton.onClick.AddListener(RedoLevel);

        if (_noLivesWindowXButton != null)
            _noLivesWindowXButton.onClick.AddListener(CloseWindow);

        if (nextLevelButton != null)
        {
            nextLevelButton.onClick.AddListener(GoToNextLevel);

            // Hide "Next Level" if this is the last level
            var current = _bootstrapper.SelectedLevel;
            int currentIndex = allLevels.Levels.IndexOf(current);
            if (currentIndex < 0 || currentIndex >= allLevels.Levels.Count - 1)
                nextLevelButton.gameObject.SetActive(false);
        }
    }

    private void GoToMenu()
    {
        //SceneManager.LoadScene("MainMenu+Shop");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade("MainMenu+Shop");
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager missing, loading scene directly.");
            SceneManager.LoadScene("MainMenu+Shop");
        }
    }

    private void RedoLevel()
    {
        if (_bootstrapper.Economy.State.currentLives <= 0)
        {
            if (_noLivesWindow != null)
            {
                _noLivesWindow.SetActive(true);
            }

            return;
        }

        SceneManager.LoadScene("Level");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade("Level");
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager missing, loading scene directly.");
            SceneManager.LoadScene("Level");
        }
    }

    private void GoToNextLevel()
    {
        if (_bootstrapper.Economy.State.currentLives <= 0)
        {
            if (_noLivesWindow != null)
            {
                _noLivesWindow.SetActive(true);
            }

            return;
        }

        var current = _bootstrapper.SelectedLevel;
        int currentIndex = allLevels.Levels.IndexOf(current);
        _bootstrapper.SelectedLevel = allLevels.Levels[currentIndex + 1];
        //SceneManager.LoadScene("Level");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.LoadSceneWithFade("Level");
        }
        else
        {
            Debug.LogWarning("SceneTransitionManager missing, loading scene directly.");
            SceneManager.LoadScene("Level");
        }
    }

    private void CloseWindow()
    {
        if (AudioManager.instance != null)
        {
            AudioManager.instance.PlaySFXPitchAdjusted(9);
        }

        _noLivesWindow.SetActive(false);
    }
}
