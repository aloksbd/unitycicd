using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class LinkedFloorPlan
{
    private static Dictionary<CreatorItem, List<CreatorItem>> linkedFloorsDictionary = new Dictionary<CreatorItem, List<CreatorItem>>();

    public static List<CreatorItem> GetChildItems(CreatorItem item)
    {
        if (linkedFloorsDictionary.ContainsKey(item))
        {
            return linkedFloorsDictionary[item];
        }
        return new List<CreatorItem>() { item };
    }
    public static void GetLinkedItems(CreatorItem item, List<CreatorItem> LinkedItemList)
    {
        if (linkedFloorsDictionary.ContainsKey(item))
        {
            foreach (var child in linkedFloorsDictionary[item])
            {
                if (child != item)
                {
                    LinkedItemList.Add(child);
                    GetLinkedItems(child, LinkedItemList);
                }
            }
        }
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

    public static bool Remove(CreatorItem baseItem)
    {
        return linkedFloorsDictionary.Remove(baseItem);
    }

    public static CreatorItem GetParentItem(CreatorItem linkedItem)
    {
        return linkedFloorsDictionary.FirstOrDefault(x => x.Value.Contains(linkedItem) && x.Key != linkedItem).Key;
    }
}