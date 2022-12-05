using System;

public class AddParentCreatorItemCommand : ICreatorItemCommand
{
    protected Guid _id;
    protected Guid _parentid;
    public Guid Id { get => _id; }

    public AddParentCreatorItemCommand(Guid id, Guid parentid)
    {
        _id = id;
        _parentid = parentid;
    }

    public virtual void Execute()
    {
        try
        {
            var child = CreatorItemFinder.FindById(_id);
            var parent = CreatorItemFinder.FindById(_parentid);
            parent.AddChild(child);
        }
        catch
        {
            Trace.Log("Creator Item with given id not found");
        }
    }

    public virtual void UnExecute()
    {
        try
        {
            var child = CreatorItemFinder.FindById(_id);
            var parent = CreatorItemFinder.FindById(_parentid);
            child.RemoveFromParent();
        }
        catch
        {
            Trace.Log("Creator Item with given id not found");
        }
    }
}