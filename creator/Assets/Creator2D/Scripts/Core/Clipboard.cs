using System.Collections.Generic;
using System;

namespace ObjectModel
{
    public class Clipboard
    {
        private Clipboard() { }
        public static Clipboard Instance { get { return Nested.instance; } }

        private Guid _sourceId;
        private List<IItem> _items = new List<IItem>();
        public List<IItem> Items { get => _items; }

        public void CopyToClipboard(List<IItem> items)
        {
            // saving clone so the state when copied is pasted
            _items = GetClonedIItems(items);
        }

        public List<IItem> PasteFromClipboard()
        {
            return GetClonedIItems(_items);
        }

        private List<IItem> GetClonedIItems(List<IItem> items)
        {
            List<IItem> clonedItems = new List<IItem>();
            foreach (var item in items)
            {
                clonedItems.Add(((IClonable)item).Clone());
            }
            return clonedItems;
        }

        private class Nested
        {
            static Nested() { }

            internal static readonly Clipboard instance = new Clipboard();
        }
    }
}
