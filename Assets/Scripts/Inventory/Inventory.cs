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
    public void CheckoutItem(Item item)
    {
        if (!items.Contains(item))
        {
            Debug.LogWarning("Item not found in inventory.");
            return;
        }

        EventManager.GetEvent<OnRecycleEvent>().Invoke(new OnRecycleEvent(item));
        items.Remove(item);

        Debug.Log($"Item checked out: {item.Name} for {item.Value} points.");
    }

    public void CheckoutAll()
    {
        foreach (var item in items)
        {
            CheckoutItem(item);
        }
    }

}