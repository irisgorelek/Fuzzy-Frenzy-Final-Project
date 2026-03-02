using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementCardData : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text progressNumberText;
    [SerializeField] private Image fillImage;
    [SerializeField] private GameObject completedStamp;

    public void SetData(AchievementSO achievement, int current, int goal)
    {
        titleText.text = achievement.Title;
        descriptionText.text = achievement.Description;

        progressNumberText.text = $"{current}/{goal}";
        fillImage.fillAmount = goal > 0 ? (float)current / goal : 0f;

        bool isCompleted = current >= goal;
        completedStamp.SetActive(isCompleted);
    }
}
