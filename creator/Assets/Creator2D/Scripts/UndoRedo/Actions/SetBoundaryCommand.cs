using System.Collections.Generic;
using System;
using UnityEngine;

public class SetBoundaryCommand : ICreatorItemCommand
{
    private Guid _id;
    private List<Vector3> _boundary;
    private List<Vector3> _previousBoundary;
    public Guid Id { get => _id; }

    public SetBoundaryCommand(Guid id, List<Vector3> boundary)
    {
        _id = id;
        _boundary = boundary;

    }

    public void Execute()
    {
        try
        {
            var item = CreatorItemFinder.FindById(_id);
            var hasBoundary = item.GetComponent<NewIHasBoundary>();
            _previousBoundary = hasBoundary.Boundary;
            hasBoundary.SetBoundary(_boundary);
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
            var hasBoundary = item.GetComponent<NewIHasBoundary>();
            hasBoundary.SetBoundary(_previousBoundary);
        }
        catch
        {
            Trace.Log("Creator Item with given id not found");
        }
    }
}