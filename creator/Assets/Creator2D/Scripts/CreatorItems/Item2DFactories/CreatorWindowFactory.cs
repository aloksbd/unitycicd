using UnityEngine;

public class CreatorWindowFactory : ICreatorItemFactory
{
    private CreatorItem _parentItem;
    private Vector3 _startPosition;
    private Sprite _sprite;

    public CreatorWindowFactory(CreatorItem parentItem, Vector3 startPosition, Sprite sprite)
    {
        _parentItem = parentItem;
        _startPosition = startPosition;
        _sprite = sprite;
    }

    public CreatorItem Create(string name, bool createGO = true)
    {
        NewWindow item;
        GameObject window = null;
        if (createGO)
        {
            UIItem uiItem = new WindowUIFactory().Create(name);
            window = CreateWindow();
            window.transform.eulerAngles = new Vector3(0, 0, _parentItem.gameObject.transform.eulerAngles.z);
            item = new NewWindow(window, uiItem);
        }
        else
        {
            item = new NewWindow(null, null);
        }
        var parentPos = _parentItem.GetComponent<NewIHasPosition>().Position;
        item.SetDimension(WHConstants.DefaultWindowLength, WHConstants.DefaultWindowHeight, WHConstants.DefaultWindowBreadth);
        item.SetPosition(new Vector3(Mathf.Abs(_startPosition.x - parentPos.x), 0, WHConstants.DefaultWindowY));
        item.SetName(name);
        item.SetRotation(0, 0, 0);
        if (item.uiItem != null)
        {
            item.uiItem._delegate = item;
        }
        if (createGO)
        {
            WallObjectTransformHandler transformHandler = new WallObjectTransformHandler(window, item, WHConstants.WINDOW);
        }
        return item;
    }

    private GameObject CreateWindow()
    {
        GameObject windowGO = new GameObject();
        SpriteRenderer spriteRenderer = windowGO.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = _sprite;

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