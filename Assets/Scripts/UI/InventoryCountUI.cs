using System;
using TMPro;
using UnityEngine;

public class InventoryCountUI : MonoBehaviour
{
    public GameObject inventoryCountPanel;
    public TextMeshProUGUI itemCountText;

    private void Start()
    {
        UpdateItemCount();
        EventManager.AddListener<OnItemSelectedEvent>((e) => UpdateItemCount(), Priority.Result);
        EventManager.AddListener<OnRecycleEvent>((e) => UpdateItemCount(true), Priority.Result);
    }

    private void OnDisable()
    {
        EventManager.RemoveListener<OnItemSelectedEvent>((e) => UpdateItemCount());
        EventManager.RemoveListener<OnRecycleEvent>((e) => UpdateItemCount(true));
    }

    public void UpdateItemCount(bool minusOne = false)
    {
        int itemCount = Inventory.Instance.GetItems().Count;
        itemCount = Math.Clamp(itemCount, 0, 999) - (minusOne ? 1 : 0);

        if (itemCount == 0)
        {
            inventoryCountPanel.SetActive(false);
            itemCountText.text = "";
        }
        else
        {
            inventoryCountPanel.SetActive(true);
            itemCountText.text = itemCount.ToString();
        }
    }
}