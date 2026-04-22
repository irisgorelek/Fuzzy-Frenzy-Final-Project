using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelButtonUI : MonoBehaviour
{
    [SerializeField] private TMP_Text levelNumberText;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private Button button;

    [Header("Stars")]
    [SerializeField] private GameObject star1;
    [SerializeField] private GameObject star2;
    [SerializeField] private GameObject star3;

    public void SetData(int levelNumber, bool unlocked, int starCount, Action onClick)
    {
        levelNumberText.text = levelNumber.ToString();

        button.interactable = unlocked;

        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);

        levelNumberText.gameObject.SetActive(unlocked);

        if (star1 != null) star1.SetActive(starCount >= 1);
        if (star2 != null) star2.SetActive(starCount >= 2);
        if (star3 != null) star3.SetActive(starCount >= 3);

        button.onClick.AddListener(() => onClick?.Invoke());
    }
}
