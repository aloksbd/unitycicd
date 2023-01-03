using UnityEngine;
public class CreatorFloorPlanFactory : ICreatorItemFactory
{
    public CreatorItem Create(string name, bool createGO = true)
    {
        CreatorItem item;
        if (createGO)
        {
            UIItem uiItem = new FloorPlanUIFactory().Create(name);
            GameObject building = new GameObject(name);
            item = new CreatorFloorPlanItem(building, uiItem);
        }
        else
        {
            item = new CreatorFloorPlanItem(null, null);
        }
        item.SetName(name);
        if (item.uiItem != null)
        {
            item.uiItem._delegate = item;
        }
        return item;
    }
}