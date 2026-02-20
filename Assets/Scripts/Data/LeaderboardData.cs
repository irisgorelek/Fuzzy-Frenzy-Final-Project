
using System;

[Serializable]
public class LeaderboardData
{
    public string PlayerName;
    public int PlayerScore;
    public long Timestamp;

    public LeaderboardData() { }

    public LeaderboardData(string playerName, int playerScore)
    {
        PlayerName = playerName;
        PlayerScore = playerScore;
        Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }
}
