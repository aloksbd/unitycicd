using System;
using UnityEngine;
using System.Collections.Generic;

namespace ObjectModel
{
    public class FloorType
    {
        public static Guid CARPET = Guid.NewGuid();
        public static Guid MARBEL = Guid.NewGuid();
        public static Guid CONCRETE = Guid.NewGuid();
    }

    public class Floor : Item
    {
        private Floor() : base(() => NamingStrategy.GetName(WHConstants.FLOOR)) { }
        private Floor(ITypedItem typedItem, IHasDimension dimension, Item Parent) : base(() => NamingStrategy.GetName(WHConstants.FLOOR, Parent.Children))
        {
            AddComponent(typedItem);
            AddComponent(dimension);
            AddComponent(new Selectable());

            var gameObject3d = new GameObject3D(
                Name,
                () => new Mesh(),
                () => GameObject3D.ChildrenToIGameObject3D(Children),
                () => { return new Vector3(0, 0, 0); },
                () => { return new Vector3(0, 0, 0); },
                () =>
                {
                    var material = new Material(Shader.Find("Standard"));
                    material.color = Color.white;
                    return material;
                }
            );

            AddComponent(gameObject3d);
        }

        public static Floor Create(Guid type, Item Parent)
        {
            var typedItem = new TypedItem(type);
            var dimension = new Dimension(0, 0, 0.2f);
            return new Floor(typedItem, dimension, Parent);
        }

        override protected IItem GetClonedItem()
        {
            return new Floor();
        }
    }
}