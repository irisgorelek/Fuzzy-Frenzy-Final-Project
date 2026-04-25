using UnityEngine;
using UnityEngine.UI;

public class AvatarDisplay : MonoBehaviour
{
    [Header("Avatar Layers")]
    [SerializeField] private Image bodyImage;
    [SerializeField] private Image hairImage;
    [SerializeField] private Image clothesImage;
    [SerializeField] private Image eyeColorImage;
    [SerializeField] private Image accessoryImage;

    public void ApplyItem(AvatarCategoryType categoryType, AvatarItemSO item)
    {
        switch (categoryType)
        {
            case AvatarCategoryType.Hair:
                if (item.IsAccessory)
                    SetAccessory(item);
                else
                    hairImage.sprite = item.AvatarSprite;
                break;

            case AvatarCategoryType.HairColor:
                hairImage.color = item.Color;
                break;

            case AvatarCategoryType.EyeColor:
                eyeColorImage.color = item.Color;
                break;

            case AvatarCategoryType.Clothes:
                clothesImage.sprite = item.AvatarSprite;
                break;
        }
    }

    public void SetAccessory(AvatarItemSO item)
    {
        if (accessoryImage == null) return;

        if (item != null)
        {
            accessoryImage.sprite = item.AvatarSprite;
            accessoryImage.enabled = true;
        }
        else
        {
            accessoryImage.sprite = null;
            accessoryImage.enabled = false;
        }
    }
}
