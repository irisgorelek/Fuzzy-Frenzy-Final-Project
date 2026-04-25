using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NameInputBanner : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button confirmButton;

    private void Start()
    {
        confirmButton.onClick.AddListener(OnConfirm);

        if (GameBootstrapper.Instance.Economy.HasPlayerName)
            panel.SetActive(false);
        else
            panel.SetActive(true);
    }

    private void OnConfirm()
    {
        string trimmed = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;

        GameBootstrapper.Instance.Economy.SetPlayerName(trimmed);
        panel.SetActive(false);
    }

    private void OnDestroy()
    {
        confirmButton.onClick.RemoveListener(OnConfirm);
    }
}
