using UnityEngine;
using System;

public class RotateCommand : ICreatorItemCommand
{
    private Guid _id;
    private Vector3 _eulerAngle;
    private Vector3 _previousEulerAngle;
    public Guid Id { get => _id; }

    public RotateCommand(Guid id, Vector3 eulerAngle)
    {
        _id = id;
        _eulerAngle = eulerAngle;
    }

    public void Execute()
    {
        try
        {
            var item = CreatorItemFinder.FindById(_id);
            var rotatableItem = item.GetComponent<NewIHasRotation>();
            _previousEulerAngle = rotatableItem.EulerAngles;
            rotatableItem.SetRotation(_eulerAngle.x, _eulerAngle.y, _eulerAngle.z);
        }
        catch
        {
            Trace.Log("Creator Item with given id not found");
        }
    }

    public void UnExecute()
    {
        try
        {
            var item = CreatorItemFinder.FindById(_id);
            var rotatableItem = item.GetComponent<NewIHasRotation>();
            rotatableItem.SetRotation(_previousEulerAngle.x, _previousEulerAngle.y, _previousEulerAngle.z);
        }
        catch
        {
            Trace.Log("Creator Item with given id not found");
        }
    }
}