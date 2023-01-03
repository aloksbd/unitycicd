using UnityEngine;

public class NewItemWithMesh : CreatorItem, NewIHasRotation, NewIHasDimension, NewIHasMesh
{
    private float _angle;
    private Dimension _dimension;
    private Vector3 _eulerAngles = new Vector3(0, 0, 0);
    public Vector3 EulerAngles { get => _eulerAngles; }
    public Dimension Dimension { get => _dimension; }
    public NewItemWithMesh(GameObject gameObject, UIItem uiItem) : base(gameObject, uiItem) { }
    private WallTransform _wallTransform;

    public void SetRotation(float x, float y, float z)
    {
        Quaternion target = Quaternion.Euler(x, y, z);
        // gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, target, 0);
        _eulerAngles = new Vector3(x, y, z);
    }

    public void RotateBy(float x, float y, float z)
    {
        Quaternion target = Quaternion.Euler(x, y, z);
        gameObject.transform.rotation = target;

        // rotation y in 2d is rotation z in 3d
        _eulerAngles += new Vector3(x, z, y);
    }

    public virtual void SetDimension(float length, float height, float width)
    {
        _dimension = new Dimension(length, height, width);
    }

    public virtual Mesh GetMesh()
    {
        return new NewWallCreator(_dimension.Height, _dimension.Length, _dimension.Width, children).CreateWallMesh();
    }

    public virtual void SetWallTransformer()
    {
        _wallTransform = new WallTransform(gameObject, this);
    }

    public virtual WallTransform GetWallTranformer()
    {
        if (_wallTransform == null)
        {
            SetWallTransformer();
        }
        return _wallTransform;
    }

    public override void Select()
    {
        base.Select();
        NewSelectedItem.Instance.AddForMultiSelection(this);
    }

    public override void Deselect()
    {
        base.Deselect();
        NewSelectedItem.Instance.DeSelect(this);
    }

    private bool _Inside(float itemX, float itemLength, float targetPositionX, float targetLength)
    {
        if (itemX >= targetPositionX && (itemX + itemLength) <= (targetPositionX + targetLength))
        {
            return true;
        }
        return false;
    }

    private bool _OutsideItem(float itemX, float itemLength, float targetPositionX, float targetLength)
    {
        if (itemX >= (targetPositionX + targetLength) || (itemX + itemLength) <= targetPositionX)
        {
            return true;
        }
        return false;
    }

    public bool CanAddItem(Vector3 position, float itemLength)
    {
        var dist = Vector3.Distance(position, Position);
        var itemX = Position.x + dist;
        if (!_Inside(itemX, itemLength, this.Position.x, this.Dimension.Length))
        {
            return false;
        }

        foreach (var child in this.children)
        {

            if (child is NewItemWithMesh)
            {
                if (!_OutsideItem(itemX, itemLength, child.Position.x + this.Position.x, ((NewItemWithMesh)child).Dimension.Length))
                {
                    return false;
                }
            }
        }

        return true;
    }

    public override CreatorItem Clone()
    {
        CreatorItem clone = new NewItemWithMesh(GameObject.Instantiate(gameObject), new UIItem(name));
        clone.SetName(name);
        clone.SetPosition(this.Position);
        clone.GetComponent<NewIHasRotation>().SetRotation(this._eulerAngles.x, this._eulerAngles.y, this._eulerAngles.z);
        clone.GetComponent<NewIHasDimension>().SetDimension(_dimension.Length, _dimension.Height, _dimension.Width);
        CloneChildren(clone);
        return clone;
    }
}
