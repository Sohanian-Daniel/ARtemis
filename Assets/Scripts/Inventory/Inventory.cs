using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance;

    [SerializeField]
    private List<Item> items = new List<Item>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ClearInventory()
    {
        items.Clear();
        Debug.Log("Inventory cleared.");
    }

    public void AddItem(ClassificationResult result)
    {
        items.Add(new Item(result));
        Debug.Log($"Item added to inventory: {result.ClassName}");
    }

    public List<Item> GetItems()
    {
        return items;
    }

    // Confirm and process the items from inventory and actually grant the rewards
    public void Checkout()
    {
        var onRecycleEvent = EventManager.GetEvent<OnRecycleEvent>();

        int totalValue = 0;
        foreach (var item in items)
        {
            totalValue += item.Value;

            onRecycleEvent.Invoke(new OnRecycleEvent(item));
        }

        Debug.Log($"Checkout complete. Total value of items: {totalValue} points.");
        ClearInventory();

    }

}