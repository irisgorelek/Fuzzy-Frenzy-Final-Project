using UnityEngine;
using UnityEngine.UI;

public class AvatarDisplay : MonoBehaviour
{
    [Header("Avatar Layers")]
    [SerializeField] private Image bodyImage;
    [SerializeField] private Image hairImage;
    [SerializeField] private Image clothesImage;
    [SerializeField] private Image eyeColorImage;

    public void ApplyItem(AvatarCategoryType categoryType, AvatarItemSO item)
    {
        switch (categoryType)
        {
            case AvatarCategoryType.Hair:
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
}
