using System;

namespace ObjectModel
{
    public interface ITypedItem : IItemComponent
    {
        Guid Type { get; }
        void SetType(Guid type);
    }

    public class TypedItem : ITypedItem
    {
        private Guid _type;
        public Guid Type
        {
            get => _type;
        }

        public TypedItem(Guid type)
        {
            _type = type;
        }

        public void SetType(Guid type)
        {
            _type = type;
        }

        public IItemComponent Clone()
        {
            return new TypedItem(_type);
        }
    }
}