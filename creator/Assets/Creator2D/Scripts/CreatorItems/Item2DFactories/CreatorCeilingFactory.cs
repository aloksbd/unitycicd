using UnityEngine;

public class CreatorCeilingFactory : ICreatorItemFactory
{
    public CreatorItem Create(string name)
    {
        UIItem uiItem = new CeilingUIFactory().Create(name);
        GameObject ceiling = new GameObject(name);
        ceiling.transform.parent = SceneObject.Find(SceneObject.Mode.Creator).transform;
        CreatorItem item = new NewCeiling(ceiling, uiItem);
        item.SetName(name);
        item.uiItem._delegate = item;
        return item;
    }
}