using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementUIController : MonoBehaviour
{
    // A prefab for the element
    public GameObject achievementUIPrefab;

    // A parent transform to hold the elements (e.g., a vertical layout group, scroll view content, etc.)
    public Transform contentUIParent;

    private VerticalLayoutGroup contentLayoutGroup;

    public TextMeshProUGUI pointsLabel;
    public TextMeshProUGUI itemsRecycledLabel;


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

        pointsLabel.text = "Total Points " + ProgressionManager.Instance.userPoints.ToString();
        itemsRecycledLabel.text = "Items Recycled " + ProgressionManager.Instance.totalItemsRecycled.ToString();

        // Fetch achievements
        var achievements = ProgressionManager.Instance.achievements;

        // Create UI elements for each item
        foreach (var achievement in achievements)
        {
            GameObject achievementUI = Instantiate(achievementUIPrefab, contentUIParent);
            AchievementUIDisplay uiItem = achievementUI.GetComponent<AchievementUIDisplay>();
            if (uiItem != null)
            {
                uiItem.Initialize(achievement);
            }
            else
            {
                Debug.LogError("Achievement UI Prefab must have an AchievementUIItem component.");
            }
        }
    }
}