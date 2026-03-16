using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Avatar/Avatar Category", fileName = "AvatarCategory_")]
public class AvatarCategorySO : ScriptableObject
{
    [SerializeField] private string displayName;
    [SerializeField] private AvatarCategoryType categoryType;
    [SerializeField] private Sprite categoryIcon;
    [SerializeField] private List<AvatarItemSO> items;
    [SerializeField] private int defaultIndex;

    public string DisplayName => displayName;
    public AvatarCategoryType CategoryType => categoryType;
    public Sprite CategoryIcon => categoryIcon;
    public IReadOnlyList<AvatarItemSO> Items => items;
    public int DefaultIndex => Mathf.Clamp(defaultIndex, 0, items.Count - 1);
}
