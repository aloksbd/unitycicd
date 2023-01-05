using UnityEngine;

public class CreatorRoofFactory : ICreatorItemFactory
{
    public CreatorItem Create(string name = WHConstants.ROOF, bool createGO = true)
    {
        CreatorItem item;
        if (createGO)
        {
            UIItem uiItem = new RoofUIFactory().Create(name);
            GameObject roof = new GameObject(name);
            roof.transform.parent = SceneObject.Find(SceneObject.Mode.Creator).transform;
            item = new Roof(roof, uiItem);
        }
        else
        {
            item = new Roof(null, null);
        }
        item.SetName(name);
        if (item.uiItem != null)
        {
            item.uiItem._delegate = item;
        }
        return item;
    }
}