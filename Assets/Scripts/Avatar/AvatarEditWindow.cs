using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarEditWindow : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private AvatarCatalogSO catalog;
    [SerializeField] private VoidEventChannelSO avatarChangedChannel;

    [Header("Tabs")]
    [SerializeField] private Transform tabContainer;
    [SerializeField] private GameObject tabPrefab;

    [Header("Content")]
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemPrefab;

    [Header("Tab Visuals")]
    [SerializeField] private Sprite tabActive;
    [SerializeField] private Sprite tabInactive;

    [Header("Avatar Display")]
    [SerializeField] private AvatarDisplay avatarDisplay;

    [Header("Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private readonly List<AvatarTabButton> _tabs = new();
    private readonly List<AvatarItemButton> _items = new();
    private readonly Dictionary<AvatarCategoryType, int> _tempSelections = new();
    private readonly Dictionary<AvatarCategoryType, int> _savedSelections = new();

    private int _activeTabIndex = -1;
    private GameBootstrapper bootstrapper;
    private string _tempAccessoryId = "";
    private string _savedAccessoryId = "";

    public event Action<AvatarCategoryType, AvatarItemSO> OnItemSelected;

    private void Start()
    {
        bootstrapper = GameBootstrapper.Instance;
        LoadSelections();
        BuildTabs();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);

        if (catalog.Categories.Count > 0)
            SelectTab(0);

        ApplyAllToDisplay();

        if (bootstrapper != null)
            bootstrapper.Economy.OnChanged += RefreshCurrentTab;
    }

    private void OnDestroy()
    {
        if (bootstrapper != null)
            bootstrapper.Economy.OnChanged -= RefreshCurrentTab;
    }

    private void RefreshCurrentTab()
    {
        if (_activeTabIndex < 0 || _activeTabIndex >= catalog.Categories.Count)
            return;

        var category = catalog.Categories[_activeTabIndex];
        var unlocked = bootstrapper.Economy.State.unlockedAvatarItems;
        int selectedIndex = _tempSelections.GetValueOrDefault(category.CategoryType, 0);

        for (int i = 0; i < _items.Count && i < category.Items.Count; i++)
        {
            var item = category.Items[i];
            bool isColorCategory = category.CategoryType == AvatarCategoryType.HairColor
                                || category.CategoryType == AvatarCategoryType.EyeColor;
            bool isLocked = (item.IsPurchasable || item.IsAccessory) && !unlocked.Contains(item.ItemId);
            _items[i].Setup(item, isColorCategory, isLocked);

            bool isSelected = item.IsAccessory
                ? item.ItemId == _tempAccessoryId
                : i == selectedIndex;
            _items[i].SetSelected(isSelected);
        }
    }

    private void LoadSelections()
    {
        if (bootstrapper != null)
        {
            foreach (var kvp in bootstrapper.Economy.State.avatarSelections)
                _savedSelections[kvp.Key] = kvp.Value;

            _savedAccessoryId = bootstrapper.Economy.State.equippedAccessoryId ?? "";
        }

        // Fill defaults for any category not yet saved
        foreach (var category in catalog.Categories)
        {
            if (!_savedSelections.ContainsKey(category.CategoryType))
                _savedSelections[category.CategoryType] = category.DefaultIndex;
        }

        // Copy saved into temp
        foreach (var kvp in _savedSelections)
            _tempSelections[kvp.Key] = kvp.Value;

        _tempAccessoryId = _savedAccessoryId;
    }

    private void BuildTabs()
    {
        foreach (Transform child in tabContainer)
            Destroy(child.gameObject);
        _tabs.Clear();

        for (int i = 0; i < catalog.Categories.Count; i++)
        {
            var category = catalog.Categories[i];
            var tabObj = Instantiate(tabPrefab, tabContainer);
            var tab = tabObj.GetComponent<AvatarTabButton>();
            tab.Setup(category.CategoryIcon, category.DisplayName);

            int index = i;
            tab.Button.onClick.AddListener(() => SelectTab(index));
            _tabs.Add(tab);
        }
    }

    private void SelectTab(int index)
    {
        if (_activeTabIndex == index) return;
        _activeTabIndex = index;

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFXPitchAdjusted(19);

        for (int i = 0; i < _tabs.Count; i++)
            _tabs[i].SetActive(i == index, tabActive, tabInactive);

        PopulateItems(catalog.Categories[index]);
    }

    private void PopulateItems(AvatarCategorySO category)
    {
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);
        _items.Clear();

        var unlocked = bootstrapper.Economy.State.unlockedAvatarItems;
        int selectedIndex = _tempSelections.GetValueOrDefault(category.CategoryType, 0);

        for (int i = 0; i < category.Items.Count; i++)
        {
            var item = category.Items[i];
            var itemObj = Instantiate(itemPrefab, itemContainer);
            var itemBtn = itemObj.GetComponent<AvatarItemButton>();

            bool isColorCategory = category.CategoryType == AvatarCategoryType.HairColor
                                || category.CategoryType == AvatarCategoryType.EyeColor;
            bool isLocked = (item.IsPurchasable || item.IsAccessory) && !unlocked.Contains(item.ItemId);
            itemBtn.Setup(item, isColorCategory, isLocked);

            bool isSelected = item.IsAccessory
                ? item.ItemId == _tempAccessoryId
                : i == selectedIndex;
            itemBtn.SetSelected(isSelected);

            int itemIndex = i;
            itemBtn.Button.onClick.AddListener(() => OnItemClicked(category, itemIndex));
            _items.Add(itemBtn);
        }
    }

    private void OnItemClicked(AvatarCategorySO category, int index)
    {
        if (_items[index].Locked) return;

        var clickedItem = category.Items[index];

        if (clickedItem.IsAccessory)
        {
            bool wasEquipped = _tempAccessoryId == clickedItem.ItemId;
            _tempAccessoryId = wasEquipped ? "" : clickedItem.ItemId;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = category.Items[i];
                if (item.IsAccessory)
                    _items[i].SetSelected(item.ItemId == _tempAccessoryId);
            }

            if (avatarDisplay != null)
            {
                if (wasEquipped)
                    avatarDisplay.SetAccessory(null);
                else
                    avatarDisplay.SetAccessory(clickedItem);
            }
        }
        else
        {
            _tempSelections[category.CategoryType] = index;

            for (int i = 0; i < _items.Count; i++)
            {
                var item = category.Items[i];
                if (!item.IsAccessory)
                    _items[i].SetSelected(i == index);
            }

            if (avatarDisplay != null)
                avatarDisplay.ApplyItem(category.CategoryType, clickedItem);
        }

        OnItemSelected?.Invoke(category.CategoryType, clickedItem);

        if (AudioManager.instance != null)
            AudioManager.instance.PlaySFXPitchAdjusted(20);
    }

    public void Confirm()
    {
        _savedSelections.Clear();
        foreach (var kvp in _tempSelections)
            _savedSelections[kvp.Key] = kvp.Value;
        _savedAccessoryId = _tempAccessoryId;

        bootstrapper.Economy.State.avatarSelections = new Dictionary<AvatarCategoryType, int>(_savedSelections);
        bootstrapper.Economy.State.equippedAccessoryId = _savedAccessoryId;
        bootstrapper.Economy.Save();

        avatarChangedChannel?.RaiseEvent();
    }

    public void Cancel()
    {
        _tempSelections.Clear();
        foreach (var kvp in _savedSelections)
            _tempSelections[kvp.Key] = kvp.Value;
        _tempAccessoryId = _savedAccessoryId;

        ApplyAllToDisplay();

        if (_activeTabIndex >= 0)
            PopulateItems(catalog.Categories[_activeTabIndex]);
    }

    private void ApplyAllToDisplay()
    {
        if (avatarDisplay == null) return;

        foreach (var category in catalog.Categories)
        {
            int index = _tempSelections.GetValueOrDefault(category.CategoryType, category.DefaultIndex);
            avatarDisplay.ApplyItem(category.CategoryType, category.Items[index]);
        }

        AvatarItemSO accessory = FindAccessoryById(_tempAccessoryId);
        avatarDisplay.SetAccessory(accessory);
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
