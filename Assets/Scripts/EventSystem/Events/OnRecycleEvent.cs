public class OnRecycleEvent : EventContext
{
    public Materials recycledMaterial;

    public OnRecycleEvent() { }

    public OnRecycleEvent(Materials recycledMaterial)
    {
        this.recycledMaterial = recycledMaterial;
    }
}