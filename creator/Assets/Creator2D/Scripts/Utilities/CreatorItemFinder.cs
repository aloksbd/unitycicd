using System.Collections.Generic;
using System;
using UnityEngine;

public class CreatorItemFinder
{
    public static CreatorItem FindByName(string name)
    {
        return Find((item) => item.name == name, null);
    }

    public static CreatorItem FindByName(string name, CreatorItem parentItem)
    {
        return Find((item) => item.name == name, parentItem);
    }

    public static CreatorItem FindById(Guid id)
    {
        return Find((item) => item.Id == id, null);
    }

    public static CreatorItem FindItemWithGameObject(GameObject gameObject)
    {
        return Find((item) => item.gameObject == gameObject, null);
    }

    private static CreatorItem Find(Func<CreatorItem, bool> condition, CreatorItem parentItem)
    {
        List<CreatorItem> items = new List<CreatorItem>();
        if (parentItem == null)
        {
            items.Add(NewBuildingController.GetBuilding());
        }
        else
        {
            items.Add(parentItem);
        }
        var i = 0;
        while (i < items.Count)
        {
            CreatorItem item = items[i];
            if (condition(item)) return item;
            foreach (var child in item.children)
            {
                items.Add(child);
            }
            i++;
        }
        throw new CreatorItemNotFounndException();
    }
}

public class CreatorItemNotFounndException : Exception { }