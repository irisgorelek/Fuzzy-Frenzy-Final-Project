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
    [SerializeField] private Transform stars;

    public void SetData(int levelNumber, bool unlocked, Action onClick)
    {
        levelNumberText.text = levelNumber.ToString();

        button.interactable = unlocked;

        if (lockIcon != null)
            lockIcon.SetActive(!unlocked);

        levelNumberText.gameObject.SetActive(unlocked);

        button.onClick.AddListener(() => onClick?.Invoke());
    }
}
