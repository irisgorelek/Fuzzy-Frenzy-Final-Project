using UnityEngine;

public class LevelTutorialController : MonoBehaviour
{
    [SerializeField] private BoardConfig _cfg; // same config used by level
    [SerializeField] private UIPopupTween tutorialPopup;
    [SerializeField] private SpeechBubblePresenter speech;

    private void OnEnable()
    {
        speech.OnLineShown += HandleLine;
        speech.OnSpeechFinished += HandleSpeechFinished;
    }

    private void OnDisable()
    {
        speech.OnLineShown -= HandleLine;
        speech.OnSpeechFinished -= HandleSpeechFinished;
    }

    private void HandleLine(int lineIndex)
    {
        if (_cfg.levelIndex == 3 && lineIndex == 1)
        {
            tutorialPopup.Show();
        }
    }

    private void HandleSpeechFinished()
    {
        //GameBootstrapper.Instance.Economy.AddBooster(BoosterEffectType.TimerBomb, 1);
        GameBootstrapper.Instance.Economy.TryClaimOneTimeBoosterReward(
            OneTimeRewardId.Tutorial_Level3_TimerBomb,
            BoosterEffectType.TimerBomb,
            1
            );
        tutorialPopup.Hide();
    }
}