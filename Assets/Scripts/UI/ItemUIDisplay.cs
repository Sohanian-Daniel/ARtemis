using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemUIDisplay : MonoBehaviour
{
    // Placeholder UI elements, add whatever you need
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI descriptionLabel;
    public TextMeshProUGUI materialLabel;
    public TextMeshProUGUI valueLabel;
    public Image itemIconImage;

    public Item item;

    public void Initialize(Item item, Sprite itemSprite)
    {
        // Feel free to customize this based on your Item class structure
        nameLabel.text = item.Name;
        descriptionLabel.text = item.Description;
        materialLabel.text = item.Material.ToString();
        valueLabel.text = item.Value.ToString();

        itemIconImage.sprite = itemSprite;
        this.item = item;
    }
}