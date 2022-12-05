using UnityEngine;
public class CreatorFloorPlanFactory : ICreatorItemFactory
{
    public CreatorItem Create(string name)
    {
        UIItem uiItem = new FloorPlanUIFactory().Create(name);
        GameObject building = new GameObject(name);
        CreatorItem item = new CreatorFloorPlanItem(building, uiItem);
        item.SetName(name);
        item.uiItem._delegate = item;
        return item;
    }
}