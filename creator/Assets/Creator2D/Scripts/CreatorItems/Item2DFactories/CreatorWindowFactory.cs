using UnityEngine;

public class CreatorWindowFactory : ICreatorItemFactory
{
    private CreatorItem _parentItem;
    private Vector3 _startPosition;

    public CreatorWindowFactory(CreatorItem parentItem, Vector3 startPosition)
    {
        _parentItem = parentItem;
        _startPosition = startPosition;
    }

    public CreatorItem Create(string name)
    {
        UIItem uiItem = new WindowUIFactory().Create(name);
        GameObject window = CreateWindow();
        window.transform.eulerAngles = new Vector3(0, 0, _parentItem.gameObject.transform.eulerAngles.z);
        NewWindow item = new NewWindow(window, uiItem);
        item.SetDimension(WHConstants.DefaultWindowLength, WHConstants.DefaultWindowHeight, WHConstants.DefaultWindowBreadth);
        item.SetPosition(new Vector3(Mathf.Abs(_startPosition.x - _parentItem.gameObject.transform.position.x), 0, WHConstants.DefaultWindowY));
        item.SetName(name);
        item.SetRotation(0, 0, 0);
        uiItem._delegate = item;

        ObjectTransformHandler transformHandler = new ObjectTransformHandler(window, item, "window");
        return item;
    }

    private GameObject CreateWindow()
    {
        BuildingInventoryController buildingInventoryController = BuildingInventoryController.Get();
        GameObject windowGO = new GameObject();
        SpriteRenderer spriteRenderer = windowGO.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = buildingInventoryController.currentBlock.BlockSprite;

        Vector2 unit = ConvertCoordinate.PixelToUnit(spriteRenderer.sprite);

        BoxCollider lineCollider = windowGO.AddComponent<BoxCollider>();
        lineCollider.transform.parent = spriteRenderer.transform;
        lineCollider.size = new Vector3(unit.x, unit.y * 2f, 1f);
        lineCollider.center = new Vector3(unit.x / 2, 0.0f, 0.0f);

        windowGO.tag = WHConstants.METABLOCK;

        _startPosition.y -= WHConstants.DefaultWindow2DHeight / 2;
        windowGO.transform.position = _startPosition;
        windowGO.transform.localScale = new Vector3(WHConstants.DefaultWindowLength / unit.x, WHConstants.DefaultWindow2DHeight / unit.y, 1);
        return windowGO;
    }
}