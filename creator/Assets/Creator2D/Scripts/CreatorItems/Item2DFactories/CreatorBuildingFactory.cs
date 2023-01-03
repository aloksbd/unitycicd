using UnityEngine;
public class CreatorBuildingFactory : ICreatorItemFactory
{
    public CreatorItem Create(string name = "Building", bool createGO = true)
    {
        ICreatorItemFactory factory = new CreatorItemFactory(new FloorPlanHierarchyUIFactory());
        CreatorItem item = factory.Create(name, createGO);
        item.SetPosition(new Vector3(0.0f, 0.0f, 0f));
        return item;
    }
}