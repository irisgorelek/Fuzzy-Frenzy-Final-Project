using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectUI : MonoBehaviour
{
    [SerializeField] private LevelsData allLevels;
    [SerializeField] private Transform content;
    [SerializeField] private LevelButtonUI levelButtonPrefab;
    [SerializeField] private GameObject _outOfLivesPanel;
    private GameBootstrapper _bootstrapper;

    private void Start()
    {
        _outOfLivesPanel.SetActive(false);
        _bootstrapper = GameBootstrapper.Instance;
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
        if (_bootstrapper.Economy.State.currentLives <=0)
        {
            var tween = _outOfLivesPanel.GetComponent<UIPopupTween>();
            if (tween == null)
            {
                Debug.LogError($"No UIPopupTween found on {_outOfLivesPanel.name}");
                return;
            }

            tween.Show();
            //return;
        }

        _bootstrapper.SelectedLevel = config;
        SceneManager.LoadScene("Level");
    }
}
