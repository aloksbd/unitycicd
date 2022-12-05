using System;
using System.Collections.Generic;

namespace ObjectModel
{
    public class Item : IItem, IClonable
    {
        private Guid _id;
        public Guid Id { get => _id; }
        private string _name;
        public string Name { get => _name; }
        private List<IItem> _children = new List<IItem>();
        public List<IItem> Children { get => _children; }
        private WeakReference _parent;
        public IItem Parent
        {
            get
            {
                return _parent.IsAlive ? _parent.Target as IItem : null;
            }
        }
        private List<IItemComponent> _components = new List<IItemComponent>();
        public List<IItemComponent> Components { get => _components; }

        protected Item(Func<string> getName)
        {
            _id = Guid.NewGuid();
            _name = getName();
        }

        public void SetName(string name)
        {
            _name = name;
        }
        public void SetId(Guid id)
        {
            _id = id;
        }

        public void AddChild(IItem item)
        {
            _children.Add(item);
            item.AddParent(this);
        }
        public void InsertChild(int index, IItem item)
        {
            _children.Insert(index, item);
            item.AddParent(this);
        }

        public void RemoveChild(IItem item)
        {
            _children.Remove(item);
            // item.RemoveFromParent(); // Can we not have this somehow??
        }

        public void AddParent(IItem item)
        {
            _parent = new WeakReference(item);
        }

        // Prefer using RemoveFromParent over RemoveChild
        public void RemoveFromParent()
        {
            if (_parent.IsAlive)
            {
                IItem parent = _parent.Target as IItem;
                parent.RemoveChild(this);
                _parent.Target = null;
                GC.Collect();
            }
        }

        public void AddComponent(IItemComponent component)
        {
            _components.Add(component);
        }

        public virtual WeakReference GetComponent<Interface>()
        {
            foreach (var component in _components)
            {
                if (typeof(Interface).IsAssignableFrom(component.GetType()))
                {
                    return new WeakReference((Interface)(object)component);
                }
            }
            throw new InvalidCastException();
        }

        public IItem Clone()
        {
            // use cloned components in cloned item
            List<IItemComponent> components = new List<IItemComponent>();
            foreach (var component in _components)
            {
                components.Add(component.Clone());
            }

            var clonedItem = GetClonedItem();

            foreach (var component in components)
            {
                clonedItem.AddComponent(component);
            }
            return clonedItem;
        }

        // override for each children items
        virtual protected IItem GetClonedItem()
        {
            return this;
        }

        virtual public void Destroy()
        {
            RemoveFromParent();
        }

        private bool _Inside(float itemX, float itemLength, float targetPositionX, float targetLength)
        {
            if (itemX >= targetPositionX && (itemX + itemLength) <= (targetPositionX + targetLength))
            {
                return true;
            }
            return false;
        }

        private bool _OutsideItem(float itemX, float itemLength, float targetPositionX, float targetLength)
        {
            if (itemX >= (targetPositionX + targetLength) || (itemX + itemLength) <= targetPositionX)
            {
                return true;
            }
            return false;
        }

        public bool CanAddItem(float itemX, float itemLength)
        {
            var weakPosition = GetComponent<IHasPosition>();
            var weakDimension = GetComponent<IHasDimension>();
            if (weakPosition.IsAlive && weakDimension.IsAlive)
            {
                var position = (weakPosition.Target as IHasPosition).Position;
                var length = (weakDimension.Target as IHasDimension).Length;
                if (!_Inside(itemX, itemLength, position.x, length))
                {
                    return false;
                }

                foreach (var child in _children)
                {
                    var weakChildPosition = child.GetComponent<IHasPosition>();
                    var weakChildDimension = child.GetComponent<IHasDimension>();
                    if (weakChildPosition.IsAlive && weakChildDimension.IsAlive)
                    {
                        var childPosition = (weakChildPosition.Target as IHasPosition).Position;
                        var childLength = (weakChildDimension.Target as IHasDimension).Length;
                        if (!_OutsideItem(itemX, itemLength, childPosition.x + position.x, childLength))
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
