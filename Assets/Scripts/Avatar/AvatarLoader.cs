using UnityEngine;

public class AvatarLoader : MonoBehaviour
{
    [SerializeField] private AvatarDisplay avatarDisplay;
    [SerializeField] private AvatarCatalogSO catalog;
    [SerializeField] private GameBootstrapper bootstrapper;
    [SerializeField] private VoidEventChannelSO avatarChangedChannel;

    private void Start()
    {
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
        var selections = bootstrapper.Economy.State.avatarSelections;

        foreach (var category in catalog.Categories)
        {
            int index = selections.TryGetValue(category.CategoryType, out int saved)
                ? saved
                : category.DefaultIndex;

            avatarDisplay.ApplyItem(category.CategoryType, category.Items[index]);
        }
    }
}
