using UnityEngine;

public interface ICreatorItemFactory
{
    CreatorItem Create(string name);
}

public class CreatorItemFactory : ICreatorItemFactory
{
    private IItemUIFactory _uiFactory;
    public CreatorItemFactory(IItemUIFactory uIFactory)
    {
        _uiFactory = uIFactory;
    }

    public CreatorItem Create(string name)
    {
        UIItem uiItem = _uiFactory.Create(name);
        GameObject building = new GameObject(name);
        building.transform.parent = SceneObject.Find(SceneObject.Mode.Creator, ObjectName.BUILDING_CANVAS).transform;
        CreatorItem item = new CreatorItem(building, uiItem);
        item.SetName(name);
        item.uiItem._delegate = item;
        return item;
    }
}