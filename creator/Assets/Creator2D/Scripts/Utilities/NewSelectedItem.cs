using System.Collections.Generic;
using System;
using UnityEngine;

public class NewSelectedItem
{
    private NewSelectedItem() { }
    public static NewSelectedItem Instance { get { return Nested.instance; } }
    private List<CreatorItem> _items = new List<CreatorItem>();
    public List<CreatorItem> Items { get => _items; }

    public void Select(CreatorItem item)
    {
        Clear();
        AddForMultiSelection(item);
    }

    public void AddForMultiSelection(CreatorItem item)
    {
        _items.Add(item);
        //GenerateHarness(item);
    }

    public void Clear()
    {
        foreach (var item in _items)
        {
            DeSelect(item);
        }
    }

    public void DeSelect(CreatorItem item)
    {
        _items.Remove(item);
        RemoveHarness(item);
    }


    private class Nested
    {
        static Nested() { }

        internal static readonly NewSelectedItem instance = new NewSelectedItem();
    }

    private void GenerateHarness(CreatorItem item)
    {
        HarnessElement harnessElement = new HarnessElement(item);
    }

    private void RemoveHarness(CreatorItem item)
    {
        GameObject line2D = item.gameObject;
        GameObject addedHarness = line2D.transform.Find(line2D.name + "HarnessElement").gameObject;
        if (addedHarness != null)
        {
            GameObject.Destroy(addedHarness);
        }
    }
}

