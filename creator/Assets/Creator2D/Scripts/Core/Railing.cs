using UnityEngine;

namespace ObjectModel
{
    public class Railing : Item
    {
        private Railing() : base(() => NamingStrategy.GetName("Railing")) { }

        private Railing(IHasPosition position, IHasRotation rotation, IHasDimension dimension) : base(() => NamingStrategy.GetName("Railing"))
        {
            AddComponent(position);
            AddComponent(rotation);
            AddComponent(dimension);
            AddComponent(new Selectable());
        }

        public static Railing Create(Vector3 position, float zAngle, float length, float width, float height)
        {
            var _position = new HasPosition(position);
            var rotation = new HasRotation(new Vector3(0, 0, zAngle));
            var dimension = new Dimension(length, width, height);

            return new Railing(_position, rotation, dimension);
        }

        override protected IItem GetClonedItem()
        {
            return new Railing();
        }
    }
}
