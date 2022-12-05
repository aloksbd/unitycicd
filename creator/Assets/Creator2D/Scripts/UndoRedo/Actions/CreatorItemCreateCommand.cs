using System;

public class CreatorItemCreateCommand : ICreatorItemCommand
{
    private ICreatorItemFactory _factory;
    private string _name;
    public CreatorItem createdItem;
    private string _parentName;
    private Guid _id = Guid.NewGuid();
    public Guid Id => _id;

    public CreatorItemCreateCommand(ICreatorItemFactory factory, string name)
    {
        _factory = factory;
        _name = name;
    }

    public void Execute()
    {
        createdItem = _factory.Create(_name);
        createdItem.SetId(_id);
    }

    public void UnExecute()
    {
        try
        {
            var item = CreatorItemFinder.FindById(_id);
            item.Destroy();
        }
        catch
        {
            Trace.Log("Creator Item with given id not found");
        }
    }
}