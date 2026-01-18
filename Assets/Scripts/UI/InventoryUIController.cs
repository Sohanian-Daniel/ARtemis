using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUIController : MonoBehaviour
{
    // A prefab for the inventory item UI element
    public GameObject itemUIPrefab;

    // A parent transform to hold the inventory item UI elements (e.g., a vertical layout group, scroll view content, etc.)
    public Transform contentUIParent;

    private VerticalLayoutGroup contentLayoutGroup;

    // Since there's no other place to hold item sprites, we can use this controller as a way to load them
    [System.Serializable]
    public class MaterialSpritePair
    {
        public Materials material;
        public Sprite sprite;
    }

    public List<MaterialSpritePair> itemSprites = new();

    private void Awake()
    {
        contentLayoutGroup = contentUIParent.GetComponent<VerticalLayoutGroup>();
        if (contentLayoutGroup == null)
        {
            Debug.LogError("Content UI Parent must have a VerticalLayoutGroup component.");
        }
    }

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

    private bool isCheckingOut = false;
    public void CheckoutItems()
    {
        if (isCheckingOut)
        {
            return;
        }

        // Prevent multiple checkouts at the same time by checking if a coroutine is already running
        StartCoroutine(CheckoutItemsCoroutine());
    }

    private IEnumerator CheckoutItemsCoroutine()
    {
        // Step 1: Disable the layout group to prevent UI updates during the process
        contentLayoutGroup.enabled = false;
        isCheckingOut = true;

        // Step 2: For each item, scroll and fade it out to the right of the screen
        // When the operation is complete, checkout re-enable the layout group to move the remaining items up

        // Since below we are destroying items while iterating we collect them first to not mess up Unity's internal child struct
        List<ItemUIDisplay> itemsToRemove = new();
        foreach (Transform child in contentUIParent)
        {
            if (child.TryGetComponent<ItemUIDisplay>(out var itemUIController))
            {
                itemsToRemove.Add(itemUIController);
            }
        }

        var itemWidth = itemsToRemove[0].GetComponent<RectTransform>().rect.width;

        foreach (ItemUIDisplay item in itemsToRemove)
        {
            item.transform.DOMoveX(Screen.width + itemWidth + 100, 0.5f).SetEase(Ease.InBack);
            yield return new WaitForSeconds(0.1f);
        }

        // Make sure all items are processed before re-enabling the layout group
        yield return new WaitForSeconds(0.5f);

        // Destroy all item UI elements
        foreach (var item in itemsToRemove)
        {
            Inventory.Instance.CheckoutItem(item.item);
            Destroy(item.gameObject);
        }

        contentLayoutGroup.enabled = true;
        isCheckingOut = false;

        yield return null;
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