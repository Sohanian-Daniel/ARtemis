public class AchievementUnlockedEvent : EventContext
{
    public Achievement achievementDefinition;

    public AchievementUnlockedEvent() { }

    public AchievementUnlockedEvent(Achievement achievementDefinition)
    {
        this.achievementDefinition = achievementDefinition;
    }
}