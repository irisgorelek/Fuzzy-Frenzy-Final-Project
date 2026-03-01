using TMPro;
using UnityEngine;

public class RankCardData : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text usernameText;
    [SerializeField] private TMP_Text pointsText;

    public void SetData(int rank, string playerName, int score)
    {
        rankText.text = rank.ToString();
        usernameText.text = playerName;
        pointsText.text = $"{score:N0} points";
    }
}
