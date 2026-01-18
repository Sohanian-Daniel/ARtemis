using System.Collections.Generic;
using UnityEngine;

public class InventoryUIController : MonoBehaviour
{
    // A prefab for the inventory item UI element
    public GameObject itemUIPrefab;

    // A parent transform to hold the inventory item UI elements (e.g., a vertical layout group, scroll view content, etc.)
    public Transform contentUIParent;

    // Since there's no other place to hold item sprites, we can use this controller as a way to load them
    [System.Serializable]
    public class MaterialSpritePair
    {
        public Materials material;
        public Sprite sprite;
    }

    public List<MaterialSpritePair> itemSprites = new();

    // Method to fetch and display inventory items
    public void DisplayInventoryItems()
    {
        // Clear existing UI elements
        foreach (Transform child in contentUIParent)
        {
            Destroy(child.gameObject);
        }

        // Fetch items from the Inventory
        var items = Inventory.Instance.GetItems();
        // Create UI elements for each item
        foreach (var item in items)
        {
            GameObject itemUI = Instantiate(itemUIPrefab, contentUIParent);
            if (itemUI.TryGetComponent<ItemUIDisplay>(out var itemUIController))
            {
                itemUIController.Initialize(item, GetSpriteForMaterial(item.Material));
            }
            else
            {
                Debug.LogWarning("ItemUIPrefab does not have an ItemUIController component.");
            }
        }
    }

    public Sprite GetSpriteForMaterial(Materials material)
    {
        foreach (var pair in itemSprites)
        {
            if (pair.material == material)
            {
                return pair.sprite;
            }
        }

        return null;
    }
}