public class OnRecycleEvent : EventContext
{
    public Materials recycledMaterial;
    public Item recycledItem;

    public OnRecycleEvent() { }

    public OnRecycleEvent(Materials recycledMaterial)
    {
        this.recycledMaterial = recycledMaterial;
    }

    public OnRecycleEvent(Item recycledItem)
    {
        this.recycledItem = recycledItem;
        this.recycledMaterial = recycledItem.Material;
    }
}