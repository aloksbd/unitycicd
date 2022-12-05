using System;

namespace ObjectModel
{
    public class Ceiling : Item
    {
        private Ceiling() : base(() => NamingStrategy.GetName("Ceiling"))
        {
            AddComponent(new Selectable());
        }

        public static Ceiling Create()
        {
            return new Ceiling();
        }

        override protected IItem GetClonedItem()
        {
            return new Ceiling();
        }
    }
}