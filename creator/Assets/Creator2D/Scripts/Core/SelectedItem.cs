using System.Collections.Generic;
using System;
using UnityEngine;

namespace ObjectModel
{
    public class SelectionSource
    {
        public static Guid BUILDING_PANEL = Guid.NewGuid();
        public static Guid VIEW_2D_MOUSE = Guid.NewGuid();
    }

    public class SelectedItem
    {
        private SelectedItem() { }
        public static SelectedItem Instance { get { return Nested.instance; } }

        private Guid _sourceId;
        private List<IItem> _items = new List<IItem>();
        public List<IItem> Items { get => _items; }

        public void Select(IItem item, Guid sourceId)
        {
            Clear();
            AddForMultiSelection(item, sourceId);
        }

        public void AddForMultiSelection(IItem item, Guid sourceId)
        {
            _items.Add(item);
            var weakSelectable = item.GetComponent<ISelectable>();
            if (weakSelectable.IsAlive)
            {
                (weakSelectable.Target as ISelectable).Select();
            }
            //GenerateHarness(item);
        }

        public void Clear()
        {
            foreach (var item in _items)
            {
                DeSelect(item);
            }
        }

        public void DeSelect(IItem item)
        {
            var weakSelectable = item.GetComponent<ISelectable>();
            if (weakSelectable.IsAlive)
            {
                (weakSelectable.Target as ISelectable).Deselect();
            }
            _items.Remove(item);
            RemoveHarness(item);
        }


        private class Nested
        {
            static Nested() { }

            internal static readonly SelectedItem instance = new SelectedItem();
        }

        private void GenerateHarness(IItem item)
        {
            // HarnessElement harnessElement = new HarnessElement(item);
        }

        private void RemoveHarness(IItem item)
        {
            var itemId = item.Id.ToString();
            GameObject line2D = GameObject.Find(itemId);
            GameObject addedHarness = GameObject.Find(line2D.name + "HarnessElement");
            if (addedHarness != null)
            {
                GameObject.Destroy(addedHarness);
            }
        }
    }
}
