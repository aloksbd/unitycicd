using System;

public class CreateCreatorItemWithParentCommand : ICreatorItemCommand
{
    protected CreatorItemCreateCommand _decoratee;
    protected Guid _parentid;
    public Guid Id { get => _decoratee.Id; }

    public CreateCreatorItemWithParentCommand(CreatorItemCreateCommand creatorCommand, Guid parentid)
    {
        _decoratee = creatorCommand;
        _parentid = parentid;
    }

    public virtual void Execute()
    {
        _decoratee.Execute();
        try
        {
            var parent = CreatorItemFinder.FindById(_parentid);
            parent.AddChild(_decoratee.createdItem);
        }
        catch (CreatorItemNotFoundException)
        {
            Trace.Log("Creator Item with given id not found");
        }
    }

    public virtual void UnExecute()
    {
        _decoratee.UnExecute();
    }
}
