using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementPopupUI : MonoBehaviour
{
    [SerializeField] private UIPopupTween popupTween;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text coinRewardText;
    [SerializeField] private Image iconImage;
    [SerializeField] private float autoHideDelay = 3f;
    [SerializeField] private AchievementEventChannelSO achievementUnlockedChannel;

    private void OnEnable()
    {
        achievementUnlockedChannel.OnEventRaised += Show;
    }

    private void OnDisable()
    {
        achievementUnlockedChannel.OnEventRaised -= Show;
    }

    public void Show(AchievementSO achievement)
    {
        titleText.text = achievement.Title;
        descriptionText.text = achievement.Description;
        coinRewardText.text = achievement.CoinReward.ToString();
        iconImage.sprite = achievement.Icon;

        if (popupTween != null)
            popupTween.Show();
        else
            gameObject.SetActive(true);

        CancelInvoke(nameof(Hide));
        Invoke(nameof(Hide), autoHideDelay);
    }

    private void Hide()
    {
        if (popupTween != null)
            popupTween.Hide();
        else
            gameObject.SetActive(false);
    }
}
