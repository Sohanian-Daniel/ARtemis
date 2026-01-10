using UnityEngine;

[System.Serializable]
public class MaterialCondition : AchievementCondition
{
    public Materials material;

    public override bool Evaluate(EventContext ctx)
    {
        if (ctx is OnRecycleEvent recycleEvent)
        {
            return recycleEvent.recycledMaterial == material;
        }

        return false;
    }
}