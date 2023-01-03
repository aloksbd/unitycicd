using UnityEngine;

public class CreatorFloorFactory : ICreatorItemFactory
{
    public CreatorItem Create(string name, bool createGO = true)
    {
        CreatorItem item;
        if (createGO)
        {
            UIItem uiItem = new FloorUIFactory().Create(name);
            GameObject floor = new GameObject(name);
            floor.transform.parent = SceneObject.Find(SceneObject.Mode.Creator).transform;
            item = new NewFloor(floor, uiItem);
        }
        else
        {
            item = new NewFloor(null, null);
        }
        item.SetName(name);
        if (item.uiItem != null)
        {
            item.uiItem._delegate = item;
        }
        return item;
    }
}