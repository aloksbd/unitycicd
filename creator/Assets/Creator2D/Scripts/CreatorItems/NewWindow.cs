using UnityEngine;

public class NewWindow : NewItemWithMesh, IHas3DObject, NewIFlipable
{
    private bool _flipedHorizontal = false;
    private bool _flipedVertical = false;
    private Vector3 _scale = Vector3.one;
    public Vector3 Scale => _scale;
    public NewWindow(GameObject gameObject, UIItem uiItem) : base(gameObject, uiItem) { }

    public GameObject GetGameObject()
    {
        return PrefabFinder.Find("Window");
    }

    public override CreatorItem Clone()
    {
        var worldPosition = gameObject.transform.position;
        var worldRotation = gameObject.transform.rotation;
        var worldScale = gameObject.transform.localScale;
        CreatorItem clone = new NewWindow(GameObject.Instantiate(gameObject), new UIItem(name));
        clone.gameObject.transform.position = worldPosition;
        clone.gameObject.transform.rotation = worldRotation;
        clone.gameObject.transform.localScale = worldScale;
        clone.SetName(name);
        clone.SetPosition(this.Position);
        clone.GetComponent<NewIHasRotation>().SetRotation(this.EulerAngles.x, this.EulerAngles.y, this.EulerAngles.z);
        clone.GetComponent<NewIHasDimension>().SetDimension(Dimension.Length, Dimension.Height, Dimension.Width);

        WallObjectTransformHandler transformHandler = new WallObjectTransformHandler(clone.gameObject, clone as NewWindow, "window");

        CloneChildren(clone);
        return clone;
    }

    public void FlipHorizontal()
    {
        _scale = new Vector3(_scale.x * -1, _scale.y, _scale.z);
        _flipedHorizontal = !_flipedHorizontal;
    }

    public void FlipVertical()
    {
        _scale = new Vector3(_scale.x, _scale.y, _scale.z * -1);
        _flipedVertical = !_flipedVertical;
    }

    public void SetScale(Vector3 scale)
    {
        _scale = scale;
    }

    public void ScaleBy(float scale)
    {
        _scale *= scale;
    }

    public Vector3 GetAdjustedPositionFor3D()
    {
        Dimension dimension = GetComponent<NewIHasDimension>().Dimension;
        Vector3 position = Position;
        if (_flipedHorizontal)
        {
            position = new Vector3(Position.x + dimension.Length, Position.y, Position.z);
        }
        if (_flipedVertical)
        {
            position = new Vector3(Position.x, Position.y + dimension.Width, Position.z);
        }
        return position;
    }
}