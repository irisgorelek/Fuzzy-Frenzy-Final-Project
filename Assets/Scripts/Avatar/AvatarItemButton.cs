using UnityEngine;
using UnityEngine.UI;

public class AvatarItemButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Image selectionFrame;

    public Button Button => button;

    public void Setup(AvatarItemSO item, bool isColorCategory)
    {
        if (isColorCategory)
        {
            icon.sprite = item.Icon;
            icon.color = item.Color;
        }
        else
        {
            icon.sprite = item.Icon;
            icon.color = Color.white;
        }

        gameObject.name = $"Item_{item.DisplayName}";
    }

    public void SetSelected(bool selected)
    {
        if (selectionFrame != null)
            selectionFrame.gameObject.SetActive(selected);
    }
}
