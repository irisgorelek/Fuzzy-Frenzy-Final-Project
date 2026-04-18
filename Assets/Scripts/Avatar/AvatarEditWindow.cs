using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AvatarEditWindow : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private AvatarCatalogSO catalog;

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

    public event Action<AvatarCategoryType, AvatarItemSO> OnItemSelected;

    private void Start()
    {
        LoadSelections();
        BuildTabs();

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);
        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);

        if (catalog.Categories.Count > 0)
            SelectTab(0);

        ApplyAllToDisplay();
    }

    private void LoadSelections()
    {
        var bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        if (bootstrapper != null)
        {
            foreach (var kvp in bootstrapper.Economy.State.avatarSelections)
                _savedSelections[kvp.Key] = kvp.Value;
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

        for (int i = 0; i < _tabs.Count; i++)
            _tabs[i].SetActive(i == index, tabActive, tabInactive);

        PopulateItems(catalog.Categories[index]);
    }

    private void PopulateItems(AvatarCategorySO category)
    {
        foreach (Transform child in itemContainer)
            Destroy(child.gameObject);
        _items.Clear();

        int selectedIndex = _tempSelections.GetValueOrDefault(category.CategoryType, 0);

        for (int i = 0; i < category.Items.Count; i++)
        {
            var item = category.Items[i];
            var itemObj = Instantiate(itemPrefab, itemContainer);
            var itemBtn = itemObj.GetComponent<AvatarItemButton>();

            bool isColorCategory = category.CategoryType == AvatarCategoryType.HairColor
                                || category.CategoryType == AvatarCategoryType.EyeColor;
            itemBtn.Setup(item, isColorCategory);
            itemBtn.SetSelected(i == selectedIndex);

            int itemIndex = i;
            itemBtn.Button.onClick.AddListener(() => OnItemClicked(category, itemIndex));
            _items.Add(itemBtn);
        }
    }

    private void OnItemClicked(AvatarCategorySO category, int index)
    {
        _tempSelections[category.CategoryType] = index;

        for (int i = 0; i < _items.Count; i++)
            _items[i].SetSelected(i == index);

        var selectedItem = category.Items[index];
        OnItemSelected?.Invoke(category.CategoryType, selectedItem);

        if (avatarDisplay != null)
            avatarDisplay.ApplyItem(category.CategoryType, selectedItem);
    }

    public void Confirm()
    {
        // Commit temp to saved
        _savedSelections.Clear();
        foreach (var kvp in _tempSelections)
            _savedSelections[kvp.Key] = kvp.Value;

        // Persist
        var bootstrapper = FindFirstObjectByType<GameBootstrapper>();
        if (bootstrapper != null)
        {
            bootstrapper.Economy.State.avatarSelections = new Dictionary<AvatarCategoryType, int>(_savedSelections);
            bootstrapper.Economy.Save();
        }
    }

    public void Cancel()
    {
        // Revert temp to saved
        _tempSelections.Clear();
        foreach (var kvp in _savedSelections)
            _tempSelections[kvp.Key] = kvp.Value;

        // Revert display
        ApplyAllToDisplay();

        // Refresh current tab to show reverted selection
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
    }
}
