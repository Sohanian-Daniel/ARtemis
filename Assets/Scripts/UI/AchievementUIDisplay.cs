using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEditor;
using System;

public class AchievementUIDisplay : MonoBehaviour
{
    // Placeholder UI elements, add whatever you need
    public TextMeshProUGUI nameLabel;
    public TextMeshProUGUI descriptionLabel;
    public TextMeshProUGUI progressLabel;
    public Image itemIconImage;

    public Achievement achievement;

    public void Initialize(Achievement achievement)
    {
        nameLabel.text = achievement.name;
        descriptionLabel.text = achievement.description;
        itemIconImage.sprite = achievement.icon;
        progressLabel.text = Math.Clamp(achievement.currentProgress, 0, achievement.requiredProgress).ToString() + " / " + achievement.requiredProgress.ToString();

        this.achievement = achievement;
    }
}