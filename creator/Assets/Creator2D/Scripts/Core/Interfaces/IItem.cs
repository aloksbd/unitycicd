using System;
using System.Collections.Generic;

namespace ObjectModel
{
    public interface IItemComponent
    {
        IItemComponent Clone();
    }

    public interface IItem
    {
        Guid Id { get; }
        String Name { get; }
        List<IItem> Children { get; }
        IItem Parent { get; }
        List<IItemComponent> Components { get; }
        void SetName(string name);
        void SetId(Guid id);
        void AddChild(IItem item);
        void RemoveChild(IItem item);
        void AddParent(IItem item);
        void RemoveFromParent();
        void AddComponent(IItemComponent component);
        WeakReference GetComponent<Interface>();
        void Destroy();
    }
}
