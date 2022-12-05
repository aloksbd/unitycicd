using System;
using System.Collections.Generic;

public class PasteCommand : ICommand
{
    private List<Guid> _ids = new List<Guid>();
    private List<Guid> _clonedIds = new List<Guid>();
    public List<CreatorItem> clonedItems;

    public PasteCommand()
    {
        foreach (var item in NewClipboard.Items)
        {
            _ids.Add(item.Id);
        }
    }

    public void Execute()
    {
        var currentItems = NewClipboard.Items;
        try
        {
            List<CreatorItem> itemsToPaste = new List<CreatorItem>();
            foreach (var id in _ids)
            {
                var item = CreatorItemFinder.FindById(id);
                itemsToPaste.Add(item);
            }
            NewClipboard.CopyToClipboard(itemsToPaste);
            clonedItems = NewClipboard.PasteFromClipboard();
            foreach (var item in clonedItems)
            {
                _clonedIds.Add(item.Id);
            }
            NewClipboard.CopyToClipboard(currentItems);
        }
        catch
        {
            // TODO: better way to handle exception here!!!
            Trace.Log("Creator Item with given id not found");
        }
    }

    public void UnExecute()
    {
        try
        {
            foreach (var id in _clonedIds)
            {
                var item = CreatorItemFinder.FindById(id);
                item.Destroy();
            }
        }
        catch
        {
            Trace.Log("Creator Item with given id not found");
        }
    }
}