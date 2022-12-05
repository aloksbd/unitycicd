using System;

public class ResizeCommand : ICreatorItemCommand
{
    private Guid _id;
    private Dimension _dimension;
    private Dimension _previousDimension;
    public Guid Id { get => _id; }

    public ResizeCommand(Guid id, Dimension dimension)
    {
        _id = id;
        _dimension = dimension;
    }

    public void Execute()
    {
        try
        {
            var item = CreatorItemFinder.FindById(_id);
            var resizableItem = item.GetComponent<NewIHasDimension>();
            _previousDimension = resizableItem.Dimension;
            resizableItem.SetDimension(_dimension.Length, _dimension.Height, _dimension.Width);
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
            var resizableItem = item.GetComponent<NewIHasDimension>();
            resizableItem.SetDimension(_previousDimension.Length, _previousDimension.Height, _previousDimension.Width);
        }
        catch
        {
            Trace.Log("Creator Item with given id not found");
        }
    }
}