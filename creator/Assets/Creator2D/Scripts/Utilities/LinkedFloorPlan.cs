using UnityEngine;
using System.Collections.Generic;

public class LinkedFloorPlan
{
    private static List<List<CreatorItem>> linkedFloorsList = new List<List<CreatorItem>>();
    private static Dictionary<CreatorItem, int> linkedFloorsDictionary = new Dictionary<CreatorItem, int>();

    public static List<CreatorItem> GetLinkedItems(CreatorItem item)
    {
        if (linkedFloorsDictionary.ContainsKey(item))
        {
            return linkedFloorsList[linkedFloorsDictionary[item]];
        }
        return new List<CreatorItem>() { item };
    }

    public static void Link(CreatorItem itemToLink, CreatorItem baseItem)
    {
        if (linkedFloorsDictionary.ContainsKey(baseItem))
        {
            List<CreatorItem> linkedItems = linkedFloorsList[linkedFloorsDictionary[baseItem]];
            linkedItems.Add(itemToLink);
            linkedFloorsDictionary[itemToLink] = linkedFloorsDictionary[baseItem];
        }
        else
        {
            linkedFloorsDictionary[baseItem] = linkedFloorsList.Count;
            linkedFloorsDictionary[itemToLink] = linkedFloorsList.Count;
            linkedFloorsList.Add(new List<CreatorItem>() { itemToLink, baseItem });
        }
    }

    public static void UnLink(CreatorItem linkedItem, CreatorItem baseItem)
    {
        if (linkedFloorsDictionary.ContainsKey(baseItem))
        {
            List<CreatorItem> linkedItems = linkedFloorsList[linkedFloorsDictionary[baseItem]];
            linkedItems.Remove(linkedItem);
            linkedFloorsDictionary.Remove(linkedItem);
        }
    }
}