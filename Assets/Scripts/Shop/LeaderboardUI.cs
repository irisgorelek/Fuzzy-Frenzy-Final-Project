using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private Transform content;

    [Header("Name Input")]
    [SerializeField] private GameObject nameInputPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button nameConfirmButton;

    [Header("Rank Prefabs")]
    [SerializeField] private RankCardData goldPrefab;
    [SerializeField] private RankCardData silverPrefab;
    [SerializeField] private RankCardData bronzePrefab;
    [SerializeField] private RankCardData beigePrefab;

    private LeaderboardManager _leaderboard;
    private EconomyContext _economy;

    private void Start()
    {
        var bootstrapper = GameBootstrapper.Instance;
        _leaderboard = bootstrapper.Leaderboard;
        _economy = bootstrapper.Economy;

        nameConfirmButton.onClick.AddListener(OnConfirmName);
    }

    private void OnEnable()
    {
        if (_economy == null) return;

        if (!_economy.HasPlayerName)
        {
            nameInputPanel.SetActive(true);
            nameInputField.text = "";
        }
        else
        {
            nameInputPanel.SetActive(false);
            LoadIfReady();
        }
    }

    private void OnConfirmName()
    {
        string trimmed = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        _economy.SetPlayerName(trimmed);
        nameInputPanel.SetActive(false);
        LoadIfReady();
    }

    private void LoadIfReady()
    {
        if (_leaderboard.IsReady)
            LoadLeaderboard();
        else
            _leaderboard.OnReady += LoadLeaderboard;
    }

    private async void LoadLeaderboard()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        var scores = await _leaderboard.GetTopScores();

        for (int i = 0; i < scores.Count; i++)
        {
            int rank = i + 1;
            RankCardData prefab = rank switch
            {
                1 => goldPrefab,
                2 => silverPrefab,
                3 => bronzePrefab,
                _ => beigePrefab
            };

            var entry = Instantiate(prefab, content);
            entry.SetData(rank, scores[i].PlayerName, scores[i].PlayerScore);
        }
    }

    private void OnDestroy()
    {
        if (_leaderboard != null)
            _leaderboard.OnReady -= LoadLeaderboard;
        nameConfirmButton.onClick.RemoveListener(OnConfirmName);
    }
}
