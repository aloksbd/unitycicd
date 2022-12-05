using UnityEngine;
public class CreatorBuildingFactory : ICreatorItemFactory
{
    public CreatorItem Create(string name = "Building")
    {
        ICreatorItemFactory factory = new CreatorItemFactory(new FloorPlanHierarchyUIFactory());
        CreatorItem item = factory.Create(name);
        item.SetPosition(new Vector3(0.0f, 0.0f, 2.3f));
        return item;
    }
}