using UnityEngine;

public class CreatorFloorFactory : ICreatorItemFactory
{
    public CreatorItem Create(string name)
    {
        UIItem uiItem = new FloorUIFactory().Create(name);
        GameObject floor = new GameObject(name);
        floor.transform.parent = SceneObject.Find(SceneObject.Mode.Creator).transform;
        CreatorItem item = new NewFloor(floor, uiItem);
        item.SetName(name);
        item.uiItem._delegate = item;
        return item;
    }
}