using System.Collections.Generic;

public class NewClipboard
{
    private static List<CreatorItem> _items = new List<CreatorItem>();
    public static List<CreatorItem> Items { get => _items; }

    public static void CopyToClipboard(List<CreatorItem> items)
    {
        // saving clone so the state when copied is pasted
        _items = GetClonedCreatorItems(items);
    }

    public static List<CreatorItem> PasteFromClipboard()
    {
        return GetClonedCreatorItems(_items);
    }

    private static List<CreatorItem> GetClonedCreatorItems(List<CreatorItem> items)
    {
        List<CreatorItem> clonedItems = new List<CreatorItem>();
        foreach (var item in items)
        {
            clonedItems.Add(item.Clone());
        }
        return clonedItems;
    }
}