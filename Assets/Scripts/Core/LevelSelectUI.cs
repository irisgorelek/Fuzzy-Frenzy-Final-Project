using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectUI : MonoBehaviour
{
    [SerializeField] private LevelsData allLevels;
    [SerializeField] private Transform content;
    [SerializeField] private LevelButtonUI levelButtonPrefab;

    private GameBootstrapper _bootstrapper;

    private void Start()
    {
        _bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        LoadLevelButtons();
    }

    private void LoadLevelButtons()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        var state = _bootstrapper.Economy.State;

        for (int i = 0; i < allLevels.Levels.Count; i++)
        {
            var config = allLevels.Levels[i];
            int levelIndex = config.levelIndex;

            bool unlocked = levelIndex == 1 || state.completedLevels.Contains(levelIndex - 1);
            state.levelStars.TryGetValue(levelIndex, out int starCount);

            var buttonObj = Instantiate(levelButtonPrefab, content);
            buttonObj.SetData(levelIndex, unlocked, starCount, () => SelectLevel(config));
        }
    }

    private void SelectLevel(BoardConfig config)
    {
        _bootstrapper.SelectedLevel = config;
        SceneManager.LoadScene("Level");
    }
}
