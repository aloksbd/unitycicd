using System;

namespace ObjectModel
{
    public class Stairs : Item
    {
        public Stairs() : base(() => NamingStrategy.GetName("Stairs"))
        {
            AddComponent(new HasPosition());
            AddComponent(new HasRotation());
            AddComponent(new Selectable());
            AddComponent(new Selectable());
        }

        override protected IItem GetClonedItem()
        {
            return new Stairs();
        }
    }
}
