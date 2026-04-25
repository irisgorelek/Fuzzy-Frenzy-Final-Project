using UnityEngine;

public class AvatarLoader : MonoBehaviour
{
    [SerializeField] private AvatarDisplay avatarDisplay;
    [SerializeField] private AvatarCatalogSO catalog;
    [SerializeField] private VoidEventChannelSO avatarChangedChannel;

    private GameBootstrapper bootstrapper;

    private void Start()
    {
        bootstrapper = GameBootstrapper.Instance;
        Refresh();
    }

    private void OnEnable()
    {
        if (avatarChangedChannel != null)
            avatarChangedChannel.OnEventRaised += Refresh;
    }

    private void OnDisable()
    {
        if (avatarChangedChannel != null)
            avatarChangedChannel.OnEventRaised -= Refresh;
    }

    private void Refresh()
    {
        var state = bootstrapper.Economy.State;
        var selections = state.avatarSelections;

        foreach (var category in catalog.Categories)
        {
            int index = selections.TryGetValue(category.CategoryType, out int saved)
                ? saved
                : category.DefaultIndex;

            avatarDisplay.ApplyItem(category.CategoryType, category.Items[index]);
        }

        avatarDisplay.SetAccessory(FindAccessoryById(state.equippedAccessoryId));
    }

    private AvatarItemSO FindAccessoryById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (var category in catalog.Categories)
            foreach (var item in category.Items)
                if (item.IsAccessory && item.ItemId == id)
                    return item;

        return null;
    }
}
