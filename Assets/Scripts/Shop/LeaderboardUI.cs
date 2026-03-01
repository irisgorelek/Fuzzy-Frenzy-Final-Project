using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    [SerializeField] private Transform content;

    [Header("Rank Prefabs")]
    [SerializeField] private RankCardData goldPrefab;
    [SerializeField] private RankCardData silverPrefab;
    [SerializeField] private RankCardData bronzePrefab;
    [SerializeField] private RankCardData beigePrefab;

    private LeaderboardManager _leaderboard;

    private void Start()
    {
        _leaderboard = FindFirstObjectByType<GameBootstrapper>().Leaderboard;

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
    }
}
