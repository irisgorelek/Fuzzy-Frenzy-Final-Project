using System.Collections.Generic;
using UnityEngine;

public class ShopPanelUI : MonoBehaviour
{
    [SerializeField] private GameBootstrapper bootstrapper;
    [SerializeField] private GameObject shopPanelRoot;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private ShopItemRowUI rowPrefab;
    [SerializeField] private List<ShopItemDefinition> items;

    private readonly List<ShopItemRowUI> spawned = new();

    private void Start()
    {
        shopPanelRoot.SetActive(false);
        BuildList();
    }

    private void BuildList()
    {
        foreach (var row in spawned)
            Destroy(row.gameObject);
        spawned.Clear();

        foreach (var item in items)
        {
            var row = Instantiate(rowPrefab, contentRoot);
            row.Bind(bootstrapper, item);
            spawned.Add(row);
        }
    }

    public void Open()
    {
        bootstrapper.Economy.ApplyLifeRegen();
        shopPanelRoot.SetActive(true);
    }

    public void Close()
    {
        shopPanelRoot.SetActive(false);
    }
}
