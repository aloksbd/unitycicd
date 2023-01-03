using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LinkedFloorPlan
{
    private static Dictionary<CreatorItem, List<CreatorItem>> linkedFloorsDictionary = new Dictionary<CreatorItem, List<CreatorItem>>();

    public static List<CreatorItem> GetLinkedItems(CreatorItem item)
    {
        if (linkedFloorsDictionary.ContainsKey(item))
        {
            return linkedFloorsDictionary[item];
        }
        return new List<CreatorItem>() { item };
    }
    public static void Link(CreatorItem itemToLink, CreatorItem baseItem)
    {
        if (linkedFloorsDictionary.ContainsKey(baseItem))
        {
            List<CreatorItem> linkedItems = linkedFloorsDictionary[baseItem];
            linkedItems.Add(itemToLink);
            linkedFloorsDictionary[baseItem] = linkedItems;
        }
        else
        {
            linkedFloorsDictionary[baseItem] = new List<CreatorItem>() { itemToLink, baseItem };
        }
    }
    public static void UnLink(CreatorItem linkedItem, CreatorItem baseItem)
    {
        if (linkedFloorsDictionary.ContainsKey(baseItem))
        {
            List<CreatorItem> linkedItems = linkedFloorsDictionary[baseItem];
            linkedItems.Remove(linkedItem);
            linkedFloorsDictionary[baseItem] = linkedItems;
        }
    }

    public static CreatorItem GetParentItem(CreatorItem linkedItem)
    {
        return linkedFloorsDictionary.FirstOrDefault(x => x.Value.Contains(linkedItem) && x.Key != linkedItem).Key;
    }
}