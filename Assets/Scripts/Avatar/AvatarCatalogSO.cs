using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Avatar/Avatar Catalog", fileName = "AvatarCatalog")]
public class AvatarCatalogSO : ScriptableObject
{
    [SerializeField] private List<AvatarCategorySO> categories;

    public IReadOnlyList<AvatarCategorySO> Categories => categories;
}
