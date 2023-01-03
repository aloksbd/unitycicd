using UnityEngine;

public interface ICreatorItemFactory
{
    CreatorItem Create(string name, bool createGO = true);
}

public class CreatorItemFactory : ICreatorItemFactory
{
    private IItemUIFactory _uiFactory;
    public CreatorItemFactory(IItemUIFactory uIFactory)
    {
        _uiFactory = uIFactory;
    }

    public CreatorItem Create(string name, bool createGO = true)
    {
        CreatorItem item;
        if (createGO)
        {
            UIItem uiItem = _uiFactory.Create(name);
            GameObject building = new GameObject(name);
            building.transform.parent = SceneObject.Find(SceneObject.Mode.Creator, ObjectName.BUILDING_CANVAS).transform;
            item = new CreatorItem(building, uiItem);
        }
        else
        {
            item = new CreatorItem(null, null);
        }
        item.SetName(name);
        if (item.uiItem != null)
        {
            item.uiItem._delegate = item;
        }
        return item;
    }
}