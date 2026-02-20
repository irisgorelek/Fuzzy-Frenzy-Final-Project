using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase;
using Firebase.Database;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class LeaderboardManager
{
    private DatabaseReference _db;
    private bool _isReady;

    private const int MaxEntries = 10; // top 10 scores

    public bool IsReady => _isReady;
    public event Action OnReady;

    public LeaderboardManager() => Initialize();

    private async void Initialize()
    {
        var status = await FirebaseApp.CheckAndFixDependenciesAsync();

        if (status != DependencyStatus.Available)
        {
            Debug.LogError($"Firebase failed to initialize: {status}");
            return;
        }

        _db = FirebaseDatabase.GetInstance("https://fuzzy-frenzy-33c00-default-rtdb.europe-west1.firebasedatabase.app").RootReference;
        _isReady = true;
        OnReady?.Invoke();
        Debug.Log("Firebase is ready.");
    }

    /// <summary>
    /// Submits a new score to the leaderboard.
    /// </summary>
    public async Task AddScore(string playerName, int score)
    {
        if (!_isReady) return;

        var entry = new LeaderboardData(playerName, score);
        string key = _db.Child("leaderboard").Push().Key;
        string json = JsonUtility.ToJson(entry);

        await _db.Child("leaderboard").Child(key).SetRawJsonValueAsync(json);
        Debug.Log($"Score submitted: {playerName} - {score}");
    }

    /// <summary>
    /// Returns the top scores sorted from highest to lowest.
    /// </summary>
    public async Task<List<LeaderboardData>> GetTopScores()
    {
        if (!_isReady) return new List<LeaderboardData>();

        var snapshot = await _db.Child("leaderboard")
            .OrderByChild("PlayerScore")
            .LimitToLast(MaxEntries)
            .GetValueAsync();

        var entries = new List<LeaderboardData>();

        foreach (var child in snapshot.Children)
        {
            string json = child.GetRawJsonValue();
            var entry = JsonUtility.FromJson<LeaderboardData>(json);
            entries.Add(entry);
        }

        entries.Sort((a, b) => b.PlayerScore.CompareTo(a.PlayerScore)); // highest first

        return entries;
    }
}