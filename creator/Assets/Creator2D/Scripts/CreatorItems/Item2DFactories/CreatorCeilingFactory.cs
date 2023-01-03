using UnityEngine;

public class CreatorCeilingFactory : ICreatorItemFactory
{
    public CreatorItem Create(string name, bool createGO = true)
    {
        CreatorItem item;
        if (createGO)
        {
            UIItem uiItem = new CeilingUIFactory().Create(name);
            GameObject ceiling = new GameObject(name);
            ceiling.transform.parent = SceneObject.Find(SceneObject.Mode.Creator).transform;
            item = new NewCeiling(ceiling, uiItem);
        }
        else
        {
            item = new NewCeiling(null, null);
        }
        item.SetName(name);
        if (item.uiItem != null)
        {
            item.uiItem._delegate = item;
        }
        return item;
    }
}