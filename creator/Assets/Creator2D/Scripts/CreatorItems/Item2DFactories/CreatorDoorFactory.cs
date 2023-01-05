using UnityEngine;

public class CreatorDoorFactory : ICreatorItemFactory
{
    private CreatorItem _parentItem;
    private Vector3 _startPosition;
    private Sprite _sprite;

    public CreatorDoorFactory(CreatorItem parentItem, Vector3 startPosition, Sprite sprite)
    {
        _parentItem = parentItem;
        _startPosition = startPosition;
        _sprite = sprite;
    }

    public CreatorItem Create(string name, bool createGO = true)
    {
        NewDoor item;
        GameObject door = null;
        if (createGO)
        {
            UIItem uiItem = new DoorUIFactory().Create(name);
            door = CreateDoor();
            door.transform.eulerAngles = new Vector3(0, 0, _parentItem.gameObject.transform.eulerAngles.z);
            item = new NewDoor(door, uiItem);
        }
        else
        {
            item = new NewDoor(null, null);
        }
        var parentPos = _parentItem.GetComponent<NewIHasPosition>().Position;
        item.SetDimension(WHConstants.DefaultDoorLength, WHConstants.DefaultDoorHeight, WHConstants.DefaultDoorBreadth);
        item.SetPosition(new Vector3(Mathf.Abs(_startPosition.x - parentPos.x), 0, WHConstants.DefaultDoorY));
        item.SetName(name);
        item.SetRotation(0, 0, 0);

        if (item.uiItem != null)
        {
            item.uiItem._delegate = item;
        }
        if (createGO)
        {
            WallObjectTransformHandler transformHandler = new WallObjectTransformHandler(door, item, WHConstants.DOOR);
        }

        return item;
    }

    private GameObject CreateDoor()
    {
        GameObject doorGO = new GameObject();
        SpriteRenderer spriteRenderer = doorGO.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = _sprite;

        Vector2 unit = ConvertCoordinate.PixelToUnit(spriteRenderer.sprite);

        BoxCollider lineCollider = doorGO.AddComponent<BoxCollider>();
        lineCollider.transform.parent = spriteRenderer.transform;
        lineCollider.size = new Vector3(unit.x, unit.y * 2f, 1f);
        lineCollider.center = new Vector3(unit.x / 2, 0.0f, 0.0f);

        doorGO.tag = WHConstants.METABLOCK;

        _startPosition.y -= WHConstants.DefaultDoor2DHeight / 2;
        doorGO.transform.position = _startPosition;
        doorGO.transform.localScale = new Vector3(WHConstants.DefaultDoorLength / unit.x, WHConstants.DefaultDoor2DHeight / unit.y, 1);
        return doorGO;
    }
}