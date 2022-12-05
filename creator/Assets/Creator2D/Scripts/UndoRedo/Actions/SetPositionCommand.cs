using UnityEngine;
using System;

public class SetPositionCommand : ICreatorItemCommand
{
    private Guid _id;
    private Vector3 _position;
    private Vector3 _previousPosition;
    public Guid Id { get => _id; }

    public SetPositionCommand(Guid id, Vector3 position)
    {
        _id = id;
        _position = position;
    }

    public void Execute()
    {
        try
        {
            var item = CreatorItemFinder.FindById(_id);
            var hasPosition = item.GetComponent<NewIHasPosition>();
            _previousPosition = hasPosition.Position;
            hasPosition.SetPosition(_position);
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
            var hasPosition = item.GetComponent<NewIHasPosition>();
            hasPosition.SetPosition(_previousPosition);
        }
        catch
        {
            Trace.Log("Creator Item with given id not found");
        }
    }
}